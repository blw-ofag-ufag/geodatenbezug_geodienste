using System.Globalization;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// Handles the processing of a layer using GDAL.
/// </summary>
public class GdalLayer
{
    private const string TIdFieldName = "t_id";

    private readonly Layer inputLayer;
    private readonly Layer processingLayer;

    /// <summary>
    /// The <see cref="Layer"/> for the input data.
    /// </summary>
    public Layer ProcessingLayer => processingLayer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GdalLayer"/> class.
    /// </summary>
    public GdalLayer(Layer inputLayer, Layer processingLayer, Dictionary<string, FieldDefn> fieldTypeConversions, string[] fieldsToDrop)
    {
        this.inputLayer = inputLayer;
        this.processingLayer = processingLayer;

        var inputLayerDefinition = inputLayer.GetLayerDefn();

        using var tIdFieldDefinition = new FieldDefn(TIdFieldName, FieldType.OFTInteger);
        processingLayer.CreateField(tIdFieldDefinition, 1);

        for (var i = 0; i < inputLayerDefinition.GetFieldCount(); i++)
        {
            var originalFieldDefinition = inputLayerDefinition.GetFieldDefn(i);
            var fieldName = originalFieldDefinition.GetName();
            if (fieldName == TIdFieldName || fieldsToDrop.Contains(fieldName))
            {
                continue;
            }

            if (fieldTypeConversions != null && fieldTypeConversions.TryGetValue(fieldName, out var fieldDefinition))
            {
                processingLayer.CreateField(fieldDefinition, 1);
            }
            else
            {
                using var newFieldDefinition = new FieldDefn(fieldName, originalFieldDefinition.GetFieldType());
                newFieldDefinition.SetWidth(originalFieldDefinition.GetWidth());
                newFieldDefinition.SetPrecision(originalFieldDefinition.GetPrecision());
                processingLayer.CreateField(newFieldDefinition, 1);
            }
        }
    }

    /// <summary>
    /// Copies all features from the source layer to the target layer.
    /// </summary>
    public void CopyFeatures()
    {
        inputLayer.ResetReading();
        for (var i = 0; i < inputLayer.GetFeatureCount(1); i++)
        {
            var inputFeature = inputLayer.GetNextFeature();
            var processingLayerDefinition = processingLayer.GetLayerDefn();
            using var newFeature = new Feature(processingLayerDefinition);
            newFeature.SetGeometry(inputFeature.GetGeometryRef());

            for (var j = 0; j < processingLayerDefinition.GetFieldCount(); j++)
            {
                var processingFieldDefinition = processingLayerDefinition.GetFieldDefn(j);

                var fieldName = processingFieldDefinition.GetName();
                var fieldType = processingFieldDefinition.GetFieldType();

                if (fieldName == TIdFieldName)
                {
                    newFeature.SetField(fieldName, inputFeature.GetFID());
                    continue;
                }

                if (inputFeature.IsFieldNull(fieldName))
                {
                    continue;
                }

                if (fieldType == FieldType.OFTInteger)
                {
                    newFeature.SetField(fieldName, inputFeature.GetFieldAsInteger(fieldName));
                }
                else if (fieldType == FieldType.OFTReal)
                {
                    newFeature.SetField(fieldName, inputFeature.GetFieldAsDouble(fieldName));
                }
                else if (fieldType == FieldType.OFTDateTime)
                {
                    var inputValue = inputFeature.GetFieldAsString(fieldName);
                    if (!DateTime.TryParse(inputValue, out var inputDate))
                    {
                        var inputYear = int.Parse(inputValue, CultureInfo.InvariantCulture);
                        inputDate = new DateTime(inputYear, 1, 1);
                    }

                    newFeature.SetField(fieldName, inputDate.Year, inputDate.Month, inputDate.Day, 0, 0, 0, 0);
                }
                else
                {
                    newFeature.SetField(fieldName, inputFeature.GetFieldAsString(fieldName));
                }
            }

            processingLayer.CreateFeature(newFeature);
        }
    }

    /// <summary>
    /// Remove features from the layer that have a specific LNF code (921-928, 950, 951).
    /// </summary>
    public void FilterLnfCodes()
    {
        processingLayer.ResetReading();
        for (var i = 0; i < processingLayer.GetFeatureCount(1); i++)
        {
            var feature = processingLayer.GetNextFeature();
            var lnfCode = feature.GetFieldAsInteger("lnf_code");
            if ((lnfCode >= 921 && lnfCode <= 928) || lnfCode == 950 || lnfCode == 951)
            {
                processingLayer.DeleteFeature(feature.GetFID());
            }
        }
    }

    /// <summary>
    /// Convert multipart geometries to singlepart geometries.
    /// </summary>
    public void ConvertMultiPartToSinglePartGeometry()
    {
        processingLayer.ResetReading();
        for (var i = 0; i < processingLayer.GetFeatureCount(1); i++)
        {
            var feature = processingLayer.GetNextFeature();
            var geometry = feature.GetGeometryRef();

            if (geometry.GetGeometryCount() > 1)
            {
                for (var j = 0; j < geometry.GetGeometryCount(); j++)
                {
                    using var newFeature = feature.Clone();
                    newFeature.SetFID(-1);
                    newFeature.SetGeometry(geometry.GetGeometryRef(j));
                    processingLayer.CreateFeature(newFeature);
                }

                processingLayer.DeleteFeature(feature.GetFID());
            }
        }
    }
}
