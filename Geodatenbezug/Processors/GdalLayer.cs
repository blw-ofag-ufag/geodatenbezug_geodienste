﻿using System.Globalization;
using OSGeo.OGR;

namespace Geodatenbezug.Topics;

/// <summary>
/// Handles the processing of a layer using GDAL.
/// </summary>
public class GdalLayer
{
    private readonly Layer inputLayer;
    private readonly Layer processingLayer;

    /// <summary>
    /// The <see cref="Layer"/> for the input data.
    /// </summary>
    public Layer ProcessingLayer => processingLayer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GdalLayer"/> class.
    /// </summary>
    public GdalLayer(Layer inputLayer, Layer processingLayer, Dictionary<string, FieldType> fieldTypeConversions)
    {
        this.inputLayer = inputLayer;
        this.processingLayer = processingLayer;

        var inputLayerDefinition = inputLayer.GetLayerDefn();
        for (var i = 0; i < inputLayerDefinition.GetFieldCount(); i++)
        {
            var originalFieldDefinition = inputLayerDefinition.GetFieldDefn(i);
            FieldDefn newFieldDefinition;
            if (fieldTypeConversions != null && fieldTypeConversions.ContainsKey(originalFieldDefinition.GetName()))
            {
                newFieldDefinition = new FieldDefn(originalFieldDefinition.GetName(), fieldTypeConversions[originalFieldDefinition.GetName()]);
            }
            else
            {
                newFieldDefinition = new FieldDefn(originalFieldDefinition.GetName(), originalFieldDefinition.GetFieldType());
                newFieldDefinition.SetWidth(originalFieldDefinition.GetWidth());
                newFieldDefinition.SetPrecision(originalFieldDefinition.GetPrecision());
            }

            // TODO: Check approx_ok value
            processingLayer.CreateField(newFieldDefinition, 1);
            newFieldDefinition.Dispose();
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
            var newFeature = new Feature(processingLayerDefinition);
            newFeature.SetGeometry(inputFeature.GetGeometryRef());

            for (var j = 0; j < processingLayerDefinition.GetFieldCount(); j++)
            {
                var processingFieldDefinition = processingLayerDefinition.GetFieldDefn(j);

                var fieldName = processingFieldDefinition.GetName();
                var fieldType = processingFieldDefinition.GetFieldType();

                if (fieldType == FieldType.OFTInteger)
                {
                    newFeature.SetField(fieldName, inputFeature.GetFieldAsInteger(j));
                }
                else if (fieldType == FieldType.OFTReal)
                {
                    newFeature.SetField(fieldName, inputFeature.GetFieldAsDouble(j));
                }
                else if (fieldType == FieldType.OFTDateTime)
                {
                    var value = inputFeature.GetFieldAsString(j);
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    var dateTimeValues = value.Split("-");
                    var year = int.Parse(dateTimeValues[0], CultureInfo.InvariantCulture);
                    var month = dateTimeValues.Length > 1 ? int.Parse(dateTimeValues[1], CultureInfo.InvariantCulture) : 1;
                    var day = dateTimeValues.Length > 2 ? int.Parse(dateTimeValues[2], CultureInfo.InvariantCulture) : 1;
                    newFeature.SetField(fieldName, year, month, day, 0, 0, 0, 0);
                }
                else
                {
                    newFeature.SetField(fieldName, inputFeature.GetFieldAsString(j));
                }
            }

            processingLayer.CreateFeature(newFeature);
            newFeature.Dispose();
        }
    }

    /// <summary>
    /// Remove a field from the layer.
    /// </summary>
    public void RemoveField(string fieldName)
    {
        var fieldIndex = processingLayer.GetLayerDefn().GetFieldIndex(fieldName);
        if (fieldIndex >= 0)
        {
            processingLayer.DeleteField(fieldIndex);
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
                    var newFeature = feature.Clone();
                    newFeature.SetGeometry(geometry.GetGeometryRef(j));
                    processingLayer.CreateFeature(newFeature);
                    newFeature.Dispose();
                }

                processingLayer.DeleteFeature(feature.GetFID());
            }
        }
    }
}