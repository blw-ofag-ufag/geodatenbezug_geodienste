using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// Provides extension methods for <see cref="Layer"/>.
/// </summary>
public static class LayerExtensions
{
    /// <summary>
    /// Remove features from the layer that have a specific LNF code (921-928, 950, 951).
    /// </summary>
    public static void FilterLnfCodes(this Layer layer)
    {
        layer.ResetReading();
        for (var i = 0; i < layer.GetFeatureCount(1); i++)
        {
            var feature = layer.GetNextFeature();
            var lnfCode = feature.GetFieldAsInteger("lnf_code");
            if ((lnfCode >= 921 && lnfCode <= 928) || lnfCode == 950 || lnfCode == 951)
            {
                layer.DeleteFeature(feature.GetFID());
            }
        }
    }

    /// <summary>
    /// Convert multipart geometries to singlepart geometries.
    /// </summary>
    public static void ConvertMultiPartToSinglePartGeometry(this Layer layer)
    {
        layer.ResetReading();
        for (var i = 0; i < layer.GetFeatureCount(1); i++)
        {
            var feature = layer.GetNextFeature();
            var geometry = feature.GetGeometryRef();

            if (geometry.GetGeometryCount() > 1)
            {
                for (var j = 0; j < geometry.GetGeometryCount(); j++)
                {
                    using var newFeature = feature.Clone();
                    newFeature.SetFID(-1);
                    newFeature.SetGeometry(geometry.GetGeometryRef(j));
                    layer.CreateFeature(newFeature);
                }

                layer.DeleteFeature(feature.GetFID());
            }
        }
    }
}
