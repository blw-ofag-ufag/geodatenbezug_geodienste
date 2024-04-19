using Geodatenbezug.Topics;

namespace Geodatenbezug;

[TestClass]
[DeploymentItem("testdata/lwb_bewirtschaftungseinheit_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_biodiversitaetsfoerderflaechen_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_nutzungsflaechen_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_perimeter_ln_sf_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_perimeter_terrassenreben_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_rebbaukataster_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
public class GdalTopicTest
{
/*    [TestMethod]
    public void RebbaukatasterTest()
    {
        var result = new Rebbaukataster("C:\\Users\\rtschuemperlin\\Desktop\\BLW\\data\\BE\\lwb_rebbaukataster_v2_0_lv95_konform.gpkg").Process();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void PerimeterTerrassenrebenTest()
    {
        var result = new PerimeterTerrassenreben("C:\\Users\\rtschuemperlin\\Desktop\\BLW\\data\\BE\\lwb_perimeter_terrassenreben_v2_0_lv95_konform.gpkg").Process();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void PerimeterLnSfTest()
    {
        var result = new PerimeterLnSf("C:\\Users\\rtschuemperlin\\Desktop\\BLW\\data\\BE\\lwb_perimeter_ln_sf_v2_0_lv95_konform.gpkg").Process();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void NutzungsflaechenTest()
    {
        var result = new Nutzungsflaechen("C:\\Users\\rtschuemperlin\\Desktop\\BLW\\data\\BE\\lwb_nutzungsflaechen_v2_0_lv95_konform.gpkg").Process();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void BiodiversitaetsfoerderflaechenTest()
    {
        var result = new Biodiversitaetsfoerderflaechen("C:\\Users\\rtschuemperlin\\Desktop\\BLW\\data\\BE\\lwb_biodiversitaetsfoerderflaechen_v2_0_lv95_konform.gpkg").Process();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void BewirtschaftungseinheitTest()
    {
        var result = new Bewirtschaftungseinheit("C:\\Users\\rtschuemperlin\\Desktop\\BLW\\data\\BE\\lwb_bewirtschaftungseinheit_v2_0_lv95_konform.gpkg").Process();
        Assert.AreEqual(string.Empty, result);
    }*/
}
