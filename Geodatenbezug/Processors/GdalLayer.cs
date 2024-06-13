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

    private readonly bool filterLnfCodes;
    private readonly bool convertMultiToSinglePartGeometries;

    /// <summary>
    /// The <see cref="Layer"/> for the input data.
    /// </summary>
    public Layer ProcessingLayer => processingLayer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GdalLayer"/> class.
    /// </summary>
    public GdalLayer(Layer inputLayer, Layer processingLayer, Dictionary<string, FieldDefn> fieldTypeConversions, List<string> fieldsToDrop, bool filterLnfCodes, bool convertMultiToSinglePartGeometries)
    {
        this.inputLayer = inputLayer;
        this.processingLayer = processingLayer;

        this.filterLnfCodes = filterLnfCodes;
        this.convertMultiToSinglePartGeometries = convertMultiToSinglePartGeometries;

        var inputLayerDefinition = inputLayer.GetLayerDefn();

        using var tIdFieldDefinition = new FieldDefn(TIdFieldName, FieldType.OFTString);
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

            if (filterLnfCodes)
            {
                var lnfCode = inputFeature.GetFieldAsInteger("lnf_code");
                if ((lnfCode >= 921 && lnfCode <= 928) || lnfCode == 950 || lnfCode == 951)
                {
                    continue;
                }
            }

            var processingLayerDefinition = processingLayer.GetLayerDefn();
            using var newFeature = new Feature(processingLayerDefinition);

            for (var j = 0; j < processingLayerDefinition.GetFieldCount(); j++)
            {
                var processingFieldDefinition = processingLayerDefinition.GetFieldDefn(j);

                var fieldName = processingFieldDefinition.GetName();
                var fieldType = processingFieldDefinition.GetFieldType();

                if (fieldName == TIdFieldName)
                {
                    var fidName = inputLayer.GetFIDColumn();
                    if (inputLayer.GetFIDColumn() == TIdFieldName)
                    {
                        newFeature.SetField(fieldName, inputFeature.GetFID());
                    }
                    else
                    {
                        newFeature.SetField(fieldName, inputFeature.GetFieldAsString(TIdFieldName));
                    }

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

            if (!inputFeature.GetGeometryRef().IsValid())
            {
                throw new InvalidGeometryException(newFeature.GetFieldAsInteger(TIdFieldName));
            }

            newFeature.SetGeometry(inputFeature.GetGeometryRef());

            if (convertMultiToSinglePartGeometries)
            {
                var geometry = newFeature.GetGeometryRef();
                if (geometry.GetGeometryCount() > 1)
                {
                    for (var j = 0; j < geometry.GetGeometryCount(); j++)
                    {
                        var singlePartGeometry = geometry.GetGeometryRef(j);
                        var singlePartGeometryType = singlePartGeometry.GetGeometryType();
                        if (singlePartGeometry.IsValid() && (singlePartGeometryType == wkbGeometryType.wkbPolygon || singlePartGeometryType == wkbGeometryType.wkbCurvePolygon))
                        {
                            using var newSinglePartFeature = newFeature.Clone();
                            newSinglePartFeature.SetFID(-1);
                            newSinglePartFeature.SetGeometry(singlePartGeometry);
                            processingLayer.CreateFeature(newSinglePartFeature);
                        }
                        else
                        {
                            throw new InvalidGeometryException(newFeature.GetFieldAsInteger(TIdFieldName));
                        }
                    }
                }
                else
                {
                    processingLayer.CreateFeature(newFeature);
                }
            }
            else
            {
                processingLayer.CreateFeature(newFeature);
            }
        }
    }
}
