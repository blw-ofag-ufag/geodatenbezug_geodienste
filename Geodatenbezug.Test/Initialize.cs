using MaxRev.Gdal.Core;
using OSGeo.OGR;

namespace Geodatenbezug;

[TestClass]
public sealed class Initialize
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        GdalBase.ConfigureAll();
        Ogr.RegisterAll();
        Ogr.UseExceptions();
    }
}
