using OSGeo.OGR;

namespace Geodatenbezug.Topics;

/// <summary>
/// An abstract class that provides a template for processing a topic using GDAL.
/// </summary>
public abstract class GdalTopic(string inputFilePath)
{
    private DataSource? inputDataSource;
    private DataSource? processingDataSource;

    /// <summary>
    /// Runs the processing steps for the topic from downloading the data to creating the output file.
    /// </summary>
    /// <returns>The download link to the processed data.</returns>
    public string Process()
    {
        Initialize();
        ProcessLayers();
        CleanUp();
        return string.Empty;
    }

    private void Initialize()
    {
        Ogr.RegisterAll();
        Ogr.UseExceptions();

        // TODO: Download the file from the inputFilePath
        inputDataSource = Ogr.Open(inputFilePath, 0);
        if (inputDataSource == null)
        {
            throw new InvalidOperationException("Could not open input datasource.");
        }

        var processedFilePath = inputFilePath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture);
        if (Directory.Exists(processedFilePath))
        {
            Directory.Delete(processedFilePath, true);
        }

        var openFileGdbDriver = Ogr.GetDriverByName("OpenFileGDB");
        processingDataSource = openFileGdbDriver.CreateDataSource(processedFilePath, null);
    }

    private void CleanUp()
    {
        // TODO: Put the processed file and the original in a .zip and return the download link
        inputDataSource.Dispose();
        processingDataSource.Dispose();
    }

    /// <summary>
    /// Creates a new GDAL layer for processing.
    /// </summary>
    public GdalLayer CreateGdalLayer(string layerName, Dictionary<string, FieldType>? fieldTypeConversions)
    {
        var inputLayer = inputDataSource.GetLayerByName(layerName);
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
        var processingLayer = processingDataSource.CreateLayer(layerName, inputLayer.GetSpatialRef(), inputLayer.GetGeomType(), []);
        fieldTypeConversions ??= [];
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
        return new GdalLayer(inputLayer, processingLayer, fieldTypeConversions);
    }

    /// <summary>
    /// Contains the logic to process the layers in the topic.
    /// </summary>
    protected abstract void ProcessLayers();
}
