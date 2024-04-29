using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// Represents a processor for a specific topic.
/// </summary>
public abstract class TopicProcessor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger logger, Topic topic) : ITopicProcessor
{
    private readonly string dataDirectory = Path.Combine(Path.GetTempPath(), "Geodatenbezug", topic.Canton.ToString(), topic.BaseTopic.ToString());

    /// <summary>
    /// The directory where the data is stored for processing.
    /// </summary>
    protected string DataDirectory => dataDirectory;

    private string inputDataPath = string.Empty;

    /// <summary>
    /// The input data for processing.
    /// </summary>
    protected internal string InputDataPath
    {
        get { return inputDataPath; }
        set { inputDataPath = value; }
    }

    private DataSource? inputDataSource;

    /// <summary>
    /// The <see cref="DataSource"/> for the input data.
    /// </summary>
    protected DataSource? InputDataSource
    {
        get { return inputDataSource; }
        set { inputDataSource = value; }
    }

    private DataSource? processingDataSource;

    /// <summary>
    /// The <see cref="DataSource"/> for the processed data.
    /// </summary>
    protected DataSource? ProcessingDataSource
    {
        get { return processingDataSource; }
        set { processingDataSource = value; }
    }

    /// <summary>
    /// The geodienste.ch API.
    /// </summary>
    protected IGeodiensteApi GeodiensteApi => geodiensteApi;

    /// <summary>
    /// The logger for the processor.
    /// </summary>
    protected ILogger Logger => logger;

    /// <summary>
    /// The topic that is being processed.
    /// </summary>
    protected Topic Topic => topic;

    private readonly ProcessingResult processingResult = new ()
    {
        Code = HttpStatusCode.Processing,
        Canton = topic.Canton,
        TopicTitle = topic.TopicTitle,
    };

    /// <summary>
    /// The processing result of the topic.
    /// </summary>
    protected internal ProcessingResult ProcessingResult => processingResult;

    /// <inheritdoc />
    public async Task<ProcessingResult> ProcessAsync()
    {
        try
        {
            logger.LogInformation($"Verarbeite Thema {topic.TopicTitle} ({topic.Canton})...");

            await PrepareDataAsync().ConfigureAwait(false);

            await RunGdalProcessingAsync().ConfigureAwait(false);

            var zipFileName = $"{Path.GetFileName(dataDirectory)}_{Topic.Canton}_{DateTime.Now.ToString("yyyyMMddHHmm", new CultureInfo("de-CH"))}.zip";
            var zipFileDirectory = Path.GetDirectoryName(DataDirectory) ?? throw new InvalidOperationException("Invalid data directory");
            var zipFullFilePath = Path.Combine(zipFileDirectory, zipFileName);
            ZipFile.CreateFromDirectory(DataDirectory, zipFullFilePath);

            processingResult.DownloadUrl = await azureStorage.UploadFileAsync(Path.Combine(Topic.Canton.ToString(), zipFileName), zipFullFilePath).ConfigureAwait(false);
            processingResult.Code = HttpStatusCode.OK;
            processingResult.Reason = "Success";
            processingResult.Info = "Data processed successfully";
        }
        catch (Exception ex)
        {
            if (processingResult.Code == HttpStatusCode.Processing)
            {
                logger.LogError(ex, $"Fehler beim Verarbeiten des Themas {topic.TopicTitle} ({topic.Canton})");

                processingResult.Code = HttpStatusCode.InternalServerError;
                processingResult.Reason = ex.Message;
                processingResult.Info = ex.InnerException?.Message;
            }
        }

        return processingResult;
    }

    /// <summary>
    /// Prepares the data for processing.
    /// </summary>
    protected internal virtual async Task PrepareDataAsync()
    {
        logger.LogInformation($"Bereite Daten für die Prozessierung von {topic.TopicTitle} ({topic.Canton}) vor...");
        var downloadUrl = await ExportTopicAsync(topic).ConfigureAwait(false);
        InputDataPath = await GeodiensteApi.DownloadExportAsync(downloadUrl, DataDirectory).ConfigureAwait(false);
    }

    /// <summary>
    /// Exports the provided topic from geodienste.ch.
    /// </summary>
    protected internal async Task<string> ExportTopicAsync(Topic topic)
    {
        logger.LogInformation($"Exportiere {topic.TopicTitle} ({topic.Canton})...");

        var exportResponse = await GeodiensteApi.StartExportAsync(topic).ConfigureAwait(false);
        if (!exportResponse.IsSuccessStatusCode)
        {
            var exportResponseContent = await exportResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var errorMessage = JsonSerializer.Deserialize<GeodiensteExportError>(exportResponseContent);
            if (!errorMessage.Error.Contains(GeodiensteExportError.OnlyOneExport, StringComparison.CurrentCulture))
            {
                var errorString = exportResponse.StatusCode == HttpStatusCode.Unauthorized ? exportResponse.ReasonPhrase : errorMessage.Error;
                logger.LogError($"Fehler beim Starten des Exports für Thema {topic.TopicTitle} ({topic.Canton}): {exportResponse.StatusCode} - {errorString}");

                processingResult.Code = exportResponse.StatusCode;
                processingResult.Reason = exportResponse.ReasonPhrase;
                processingResult.Info = exportResponse.StatusCode == HttpStatusCode.Unauthorized ? string.Empty : errorMessage.Error;
                throw new InvalidOperationException("Export failed");
            }
        }

        var statusResponse = await GeodiensteApi.CheckExportStatusAsync(topic).ConfigureAwait(false);
        var statusResponseContent = await statusResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!statusResponse.IsSuccessStatusCode)
        {
            var errorMessage = JsonSerializer.Deserialize<GeodiensteStatusError>(statusResponseContent);
            logger.LogError($"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): {statusResponse.StatusCode} - {errorMessage.Error}");

            processingResult.Code = statusResponse.StatusCode;
            processingResult.Reason = statusResponse.ReasonPhrase;
            processingResult.Info = statusResponse.StatusCode == HttpStatusCode.NotFound ? errorMessage.Error : string.Empty;
            throw new InvalidOperationException("Export failed");
        }

        var statusMessage = JsonSerializer.Deserialize<GeodiensteStatusSuccess>(statusResponseContent);
        if (statusMessage.Status == GeodiensteStatus.Failed)
        {
            logger.LogError($"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): {statusMessage.Info}");

            processingResult.Code = statusResponse.StatusCode;
            processingResult.Reason = statusMessage.Status.ToString();
            processingResult.Info = statusMessage.Info;
            throw new InvalidOperationException("Export failed");
        }

        if (statusMessage.DownloadUrl == null)
        {
            logger.LogError($"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): Download-URL nicht gefunden");

            processingResult.Code = statusResponse.StatusCode;
            processingResult.Reason = statusMessage.Status.ToString();
            processingResult.Info = "Download-URL not found";
            throw new InvalidOperationException("Export failed");
        }

        return statusMessage.DownloadUrl;
    }

    /// <summary>
    /// Processes the data using GDAL.
    /// </summary>
    protected internal async Task RunGdalProcessingAsync()
    {
        logger.LogInformation($"Starte GDAL-Prozessierung von Thema {topic.TopicTitle} ({topic.Canton})...");

        Ogr.RegisterAll();
        Ogr.UseExceptions();

        InputDataSource = Ogr.Open(InputDataPath, 1);
        if (InputDataSource == null)
        {
            throw new InvalidOperationException("Could not open input datasource.");
        }

        var processedFilePath = InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture);
        if (Directory.Exists(processedFilePath))
        {
            Directory.Delete(processedFilePath, true);
        }

        var openFileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
        ProcessingDataSource = openFileGdbDriver.CreateDataSource(processedFilePath, null);

        await ProcessTopic().ConfigureAwait(false);

        InputDataSource.Dispose();
        ProcessingDataSource.Dispose();
    }

    /// <summary>
    /// Creates a new GDAL layer for processing.
    /// </summary>
    public GdalLayer CreateGdalLayer(string layerName, Dictionary<string, FieldDefn>? fieldTypeConversions)
    {
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
        return CreateGdalLayer(layerName, fieldTypeConversions, []);
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
    }

    /// <summary>
    /// Creates a new GDAL layer for processing.
    /// </summary>
    public GdalLayer CreateGdalLayer(string layerName, Dictionary<string, FieldDefn>? fieldTypeConversions, string[] fieldsToDrop)
    {
        var inputLayer = InputDataSource.GetLayerByName(layerName);

        // Workaround https://github.com/blw-ofag-ufag/geodatenbezug_geodienste/issues/45
        var geometryType = inputLayer.GetNextFeature().GetGeometryRef().GetGeometryType();

#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
        var processingLayer = ProcessingDataSource.CreateLayer(layerName, inputLayer.GetSpatialRef(), geometryType, []);
        fieldTypeConversions ??= [];
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly

        return new GdalLayer(inputLayer, processingLayer, fieldTypeConversions, fieldsToDrop);
    }

    /// <summary>
    /// Performs the actual processing of the topic.
    /// </summary>
    protected abstract Task ProcessTopic();
}
