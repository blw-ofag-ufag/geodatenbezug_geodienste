using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Geodatenbezug.Models;
using Geodatenbezug.Topics;
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

    private string inputData = string.Empty;

    /// <summary>
    /// The input data for processing.
    /// </summary>
    protected string InputData
    {
        get { return inputData; }
        set { inputData = value; }
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
    /// The topic that is being processed.
    /// </summary>
    protected Topic Topic => topic;

    private readonly ProcessingResult processingResult = new ()
    {
        Code = HttpStatusCode.Processing,
        Canton = topic.Canton,
        TopicTitle = topic.TopicTitle,
    };

    /// <inheritdoc />
    public async Task<ProcessingResult> ProcessAsync()
    {
        try
        {
            logger.LogInformation($"Verarbeite Thema {topic.TopicTitle} ({topic.Canton})...");

            await PrepareData().ConfigureAwait(false);

            await RunGdalProcessing().ConfigureAwait(false);

            var zipFileName = Path.GetFileName(DataDirectory) + ".zip";
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
    protected virtual async Task PrepareData()
    {
        logger.LogInformation($"Bereite Daten für die Prozessierung von {topic.TopicTitle} ({topic.Canton}) vor...");
        var downloadUrl = await ExportTopicAsync(topic).ConfigureAwait(false);
        InputData = await GeodiensteApi.DownloadExportAsync(downloadUrl, DataDirectory).ConfigureAwait(false);
    }

    /// <summary>
    /// Exports the provided topic from geodienste.ch.
    /// </summary>
    protected async Task<string> ExportTopicAsync(Topic topic)
    {
        var token = GetToken(topic.BaseTopic, topic.Canton);
        var exportResponse = await GeodiensteApi.StartExportAsync(topic, token).ConfigureAwait(false);
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

        var statusResponse = await GeodiensteApi.CheckExportStatusAsync(topic, token).ConfigureAwait(false);
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
    protected async Task RunGdalProcessing()
    {
        Ogr.RegisterAll();
        Ogr.UseExceptions();

        var inputFilePath = Path.Combine(dataDirectory, topic.BaseTopic.ToString() + "_v2_0_lv95.gpkg");
        InputDataSource = Ogr.Open(inputFilePath, 1);
        if (InputDataSource == null)
        {
            throw new InvalidOperationException("Could not open input datasource.");
        }

        var processedFilePath = inputFilePath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture);
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
    public GdalLayer CreateGdalLayer(string layerName, Dictionary<string, FieldType>? fieldTypeConversions)
    {
        var inputLayer = InputDataSource.GetLayerByName(layerName);

        // Workaround https://github.com/blw-ofag-ufag/geodatenbezug_geodienste/issues/45
        var geometryType = inputLayer.GetNextFeature().GetGeometryRef().GetGeometryType();

#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
        var processingLayer = ProcessingDataSource.CreateLayer(layerName, inputLayer.GetSpatialRef(), geometryType, []);
        fieldTypeConversions ??= [];
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
        return new GdalLayer(inputLayer, processingLayer, fieldTypeConversions);
    }

    /// <summary>
    /// Performs the actual processing of the topic.
    /// </summary>
    protected abstract Task ProcessTopic();

    /// <inheritdoc />
    public string GetToken(BaseTopic baseTopic, Canton canton)
    {
        var topicTokens = Environment.GetEnvironmentVariable("tokens_" + baseTopic.ToString());
        var selectedToken = topicTokens.Split(";").Where(token => token.StartsWith(canton.ToString(), StringComparison.InvariantCulture)).FirstOrDefault();
        if (selectedToken != null)
        {
            return selectedToken.Split("=")[1];
        }
        else
        {
            throw new KeyNotFoundException($"Token not found for topic {baseTopic} and canton {canton}");
        }
    }
}
