using System.Globalization;
using MaxRev.Gdal.Core;
using OSGeo.OGR;

namespace Geodatenbezug;

/// <summary>
/// Provides methods to assert the results of GDAL operations.
/// </summary>
internal static class GdalAssert
{
    /// <summary>
    /// Initializes the GDAL environment.
    /// </summary>
    internal static void Initialize()
    {
        GdalBase.ConfigureAll();
        Ogr.RegisterAll();
        Ogr.UseExceptions();
    }

    /// <summary>
    /// Asserts that the result layer has the expected fields.
    /// </summary>
    internal static void AssertLayerFields(Layer resultLayer, List<string> expectedLayerFields)
    {
        var resultLayerFields = new List<string>();
        var resultLayerDefn = resultLayer.GetLayerDefn();
        for (var i = 0; i < resultLayerDefn.GetFieldCount(); i++)
        {
            resultLayerFields.Add(resultLayerDefn.GetFieldDefn(i).GetName());
        }

        CollectionAssert.AreEqual(expectedLayerFields, resultLayerFields);
    }

    /// <summary>
    /// Asserts that the field has the expected type.
    /// </summary>
    internal static void AssertFieldType(Layer resultLayer, string fieldName, FieldType expectedFieldType)
    {
        var resultLayerDefn = resultLayer.GetLayerDefn();
        var fieldIndex = resultLayerDefn.GetFieldIndex(fieldName);
        Assert.AreEqual(resultLayerDefn.GetFieldDefn(fieldIndex).GetFieldType(), expectedFieldType);
    }

    /// <summary>
    /// Asserts that the field has the expected type and subtype.
    /// </summary>
    internal static void AssertFieldType(Layer resultLayer, string fieldName, FieldType expectedFieldType, FieldSubType expectedFieldSubType)
    {
        var resultLayerDefn = resultLayer.GetLayerDefn();
        var fieldIndex = resultLayerDefn.GetFieldIndex(fieldName);
        Assert.AreEqual(resultLayerDefn.GetFieldDefn(fieldIndex).GetFieldType(), expectedFieldType);
        Assert.AreEqual(resultLayerDefn.GetFieldDefn(fieldIndex).GetSubType(), expectedFieldSubType);
    }

    /// <summary>
    /// Asserts that the result layer contains only single-part geometries.
    /// </summary>
    internal static void AssertOnlySinglePartGeometries(Layer resultLayer)
    {
        var hasOnlySinglePartGeometries = true;
        resultLayer.ResetReading();
        for (var i = 0; i < resultLayer.GetFeatureCount(1); i++)
        {
            var feature = resultLayer.GetNextFeature();
            if (feature.GetGeometryRef().GetGeometryCount() > 1)
            {
                hasOnlySinglePartGeometries = false;
            }
        }

        Assert.IsTrue(hasOnlySinglePartGeometries);
    }

    /// <summary>
    /// Asserts that the result layer contains only valid LNF codes.
    /// </summary>
    internal static void AssertOnlyValidLnfCodes(Layer resultLayer)
    {
        var hasOnlyValidLnfCodes = true;
        resultLayer.ResetReading();
        for (var i = 0; i < resultLayer.GetFeatureCount(1); i++)
        {
            var feature = resultLayer.GetNextFeature();
            var lnfCode = feature.GetFieldAsInteger("lnf_code");
            if ((lnfCode >= 921 && lnfCode <= 928) || lnfCode == 950 || lnfCode == 951)
            {
                hasOnlyValidLnfCodes = false;
            }
        }

        Assert.IsTrue(hasOnlyValidLnfCodes);
    }

    /// <summary>
    /// Asserts that the result feature has the same datetime as the input feature.
    /// </summary>
    internal static void AssertDateTime(Feature inputFeature, Feature resultFeature, string fieldName)
    {
        var inputValue = inputFeature.GetFieldAsString(fieldName);
        if (!DateTime.TryParse(inputValue, out var inputDate))
        {
            if (string.IsNullOrEmpty(inputValue))
            {
                Assert.IsTrue(resultFeature.IsFieldNull(fieldName));
                return;
            }

            var inputYear = int.Parse(inputValue, CultureInfo.InvariantCulture);
            inputDate = new DateTime(inputYear, 1, 1);
        }

        resultFeature.GetFieldAsDateTime(fieldName, out var year, out var month, out var day, out var hour, out var minute, out var second, out var tzFlag);
        Assert.AreEqual(inputDate, new DateTime(year, month, day));
    }

    /// <summary>
    /// Asserts that the result feature has the same geometry as the input feature.
    /// </summary>
    internal static void AssertGeometry(Feature inputFeature, Feature resultFeature)
    {
        var inputCentroid = inputFeature.GetGeometryRef().Centroid();
        var resultCentroid = resultFeature.GetGeometryRef().Centroid();
        Assert.AreEqual(inputCentroid.GetX(0), resultCentroid.GetX(0), 0.0001);
        Assert.AreEqual(inputCentroid.GetY(0), resultCentroid.GetY(0), 0.0001);
    }
}
