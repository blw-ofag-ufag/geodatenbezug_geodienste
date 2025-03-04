﻿using System.Globalization;
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

    /// <summary>
    /// The input data for processing.
    /// </summary>
    protected internal string InputDataPath { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="DataSource"/> for the input data.
    /// </summary>
    protected DataSource? InputDataSource { get; set; }

    /// <summary>
    /// The <see cref="DataSource"/> for the processed data.
    /// </summary>
    protected DataSource? ProcessingDataSource { get; set; }

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

    private readonly ProcessingResult processingResult = new()
    {
        Code = HttpStatusCode.Processing,
        Canton = topic.Canton,
        TopicTitle = topic.TopicTitle,
        UpdatedAt = topic.UpdatedAt,
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
            logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Verarbeite Thema");

            await PrepareDataAsync().ConfigureAwait(false);

            await RunGdalProcessingAsync().ConfigureAwait(false);

            logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Zippe Resultate");
            var zipFileName = $"{Path.GetFileName(dataDirectory)}_{Topic.Canton}_{DateTime.Now.ToString("yyyyMMddHHmm", new CultureInfo("de-CH"))}.zip";
            var zipFileDirectory = Path.GetDirectoryName(DataDirectory) ?? throw new InvalidOperationException("Invalid data directory");
            var zipFullFilePath = Path.Combine(zipFileDirectory, zipFileName);
            ZipFile.CreateFromDirectory(DataDirectory, zipFullFilePath);

            processingResult.DownloadUrl = await azureStorage.UploadFileAsync(Path.Combine(Topic.Canton.ToString(), zipFileName), zipFullFilePath).ConfigureAwait(false);
            processingResult.Code = HttpStatusCode.OK;
            processingResult.Reason = "Success";
            processingResult.Info = "Data processed successfully";

            logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Thema erfolgreich verarbeitet. DownloadUrl: {processingResult.DownloadUrl}");

            File.Delete(zipFullFilePath);
            Directory.Delete(DataDirectory, true);
        }
        catch (Exception ex)
        {
            if (processingResult.Code == HttpStatusCode.Processing)
            {
                logger.LogError(ex, $"{topic.TopicTitle} ({topic.Canton}): Fehler beim Verarbeiten des Themas: {ex.Message}");

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
        logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Bereite Daten für die Prozessierung vor");
        var downloadUrl = await ExportTopicAsync(topic).ConfigureAwait(false);
        InputDataPath = await GeodiensteApi.DownloadExportAsync(downloadUrl, DataDirectory).ConfigureAwait(false);
    }

    /// <summary>
    /// Exports the provided topic from geodienste.ch.
    /// </summary>
    protected internal async Task<string> ExportTopicAsync(Topic topic)
    {
        logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Export");

        var exportResponse = await GeodiensteApi.StartExportAsync(topic).ConfigureAwait(false);
        if (!exportResponse.IsSuccessStatusCode)
        {
            var exportResponseContent = await exportResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var errorMessage = JsonSerializer.Deserialize<GeodiensteExportError>(exportResponseContent);
            if (!errorMessage.Error.Contains(GeodiensteExportError.OnlyOneExport, StringComparison.CurrentCulture))
            {
                var errorString = exportResponse.StatusCode == HttpStatusCode.Unauthorized ? exportResponse.ReasonPhrase : errorMessage.Error;
                logger.LogError($"{topic.TopicTitle} ({topic.Canton}): Fehler beim Starten des Exports: {exportResponse.StatusCode} - {errorString}");

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
            logger.LogError($"{topic.TopicTitle} ({topic.Canton}): Fehler bei der Statusabfrage des Datenexports: {statusResponse.StatusCode} - {errorMessage.Error}");

            processingResult.Code = statusResponse.StatusCode;
            processingResult.Reason = statusResponse.ReasonPhrase;
            processingResult.Info = statusResponse.StatusCode == HttpStatusCode.NotFound ? errorMessage.Error : string.Empty;
            throw new InvalidOperationException("Export failed");
        }

        var statusMessage = JsonSerializer.Deserialize<GeodiensteStatusSuccess>(statusResponseContent);
        if (statusMessage.Status == GeodiensteStatus.Failed)
        {
            logger.LogError($"{topic.TopicTitle} ({topic.Canton}): Fehler bei der Statusabfrage des Datenexports: {statusMessage.Info}");

            processingResult.Code = statusResponse.StatusCode;
            processingResult.Reason = statusMessage.Status.ToString();
            processingResult.Info = statusMessage.Info;
            throw new InvalidOperationException("Export failed");
        }

        if (statusMessage.DownloadUrl == null)
        {
            logger.LogError($"{topic.TopicTitle} ({topic.Canton}): Fehler bei der Statusabfrage des Datenexports: Download-URL nicht gefunden");

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
        logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Starte GDAL-Prozessierung");

        Ogr.RegisterAll();
        Ogr.UseExceptions();

        InputDataSource = Ogr.Open(InputDataPath, 1);
        if (InputDataSource == null)
        {
            logger.LogError($"Fehler beim Öffnen des Eingabedatensatzes {InputDataPath}");
            throw new InvalidOperationException("Could not open input datasource.");
        }

        var processedFilePath = InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture);
        if (Directory.Exists(processedFilePath))
        {
            Directory.Delete(processedFilePath, true);
        }

        var openFileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
        ProcessingDataSource = openFileGdbDriver.CreateDataSource(processedFilePath, null);

        await ProcessTopicAsync().ConfigureAwait(false);

        InputDataSource.Dispose();
        ProcessingDataSource.Dispose();
    }

    /// <summary>
    /// Creates a new GDAL layer for processing.
    /// </summary>
    public GdalLayer CreateGdalLayer(string layerName, Dictionary<string, FieldDefn>? fieldTypeConversions, bool filterLnfCodes, bool convertMultiToSinglePartGeometries)
    {
        return CreateGdalLayer(layerName, fieldTypeConversions, [], filterLnfCodes, convertMultiToSinglePartGeometries);
    }

    /// <summary>
    /// Creates a new GDAL layer for processing.
    /// </summary>
    public GdalLayer CreateGdalLayer(string layerName, Dictionary<string, FieldDefn>? fieldTypeConversions, List<string> fieldsToDrop, bool filterLnfCodes, bool convertMultiToSinglePartGeometries)
    {
        var inputLayer = InputDataSource.GetLayerByName(layerName) ?? throw new InvalidOperationException($"Layer {layerName} not found in input data source");

        // Workaround https://github.com/blw-ofag-ufag/geodatenbezug_geodienste/issues/45
        var geometryType = inputLayer.GetNextFeature().GetGeometryRef().GetGeometryType();

        var processingLayer = ProcessingDataSource.CreateLayer(layerName, inputLayer.GetSpatialRef(), geometryType, []);
        fieldTypeConversions ??= [];

        return new GdalLayer(inputLayer, processingLayer, fieldTypeConversions, fieldsToDrop, filterLnfCodes, convertMultiToSinglePartGeometries);
    }

    /// <summary>
    /// Performs the actual processing of the topic.
    /// </summary>
    protected abstract Task ProcessTopicAsync();
}
