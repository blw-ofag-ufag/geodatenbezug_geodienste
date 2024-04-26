using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Perimeter Ln Sf".
/// </summary>
public class PerimeterLnSfProcessor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, azureStorage, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopicAsync()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "bezugsjahr", FieldType.OFTDateTime },
        };
        var perimeterLnSfLayer = CreateGdalLayer("perimeter_ln_sf", fieldTypeConversions);
        perimeterLnSfLayer.CopyFeatures();
        perimeterLnSfLayer.ConvertMultiPartToSinglePartGeometry();

        return Task.CompletedTask;
    }
}
