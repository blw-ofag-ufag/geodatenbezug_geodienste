using System.Xml.Serialization;

namespace Geodatenbezug.Models;

[XmlRoot("TRANSFER", Namespace = "http://www.interlis.ch/INTERLIS2.3")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
public class Transfer
{
    /*    [XmlElement("HEADERSECTION")]
        public HeaderSection HeaderSection { get; set; }*/

    [XmlElement("DATASECTION")]
    required public DataSection DataSection { get; set; }
}

public class DataSection
{
    [XmlElement("LWB_Nutzungsflaechen_V2_0.LNF_Kataloge")]
    required public LnfKataloge LNFKataloge { get; set; }
}

public class LnfKataloge
{
    [XmlElement("LWB_Nutzungsflaechen_V2_0.LNF_Kataloge.LNF_Katalog_Nutzungsart")]
    required public List<LnfKatalogNutzungsart> LNFKatalogNutzungsart { get; set; }
}

public class LnfKatalogNutzungsart
{
    [XmlElement("LNF_Code")]
    required public int LNFCode { get; set; }

    [XmlElement("Nutzung")]
    required public Nutzung Nutzung { get; set; }

    [XmlElement("Hauptkategorie")]
    required public Hauptkategorie Hauptkategorie { get; set; }

    [XmlElement("Ist_BFF_QI")]
    required public bool IstBFFQI { get; set; }
}

public class Nutzung
{
    [XmlElement("LocalisationCH_V1.MultilingualText")]
    required public LocalisationCHV1MultilingualText LocalisationCHV1MultilingualText { get; set; }
}

public class Hauptkategorie
{
    [XmlElement("LocalisationCH_V1.MultilingualText")]
    required public LocalisationCHV1MultilingualText LocalisationCHV1MultilingualText { get; set; }
}

public class LocalisationCHV1MultilingualText
{
    [XmlElement("LocalisedText")]
    required public LocalisedText LocalisedText { get; set; }
}

public class LocalisedText
{
    [XmlElement("LocalisationCH_V1.LocalisedText")]
    required public List<LocalisationCHV1LocalisedText> LocalisationCHV1LocalisedText { get; set; }
}

public class LocalisationCHV1LocalisedText
{
    [XmlElement("Language")]
    required public string Language { get; set; }

    [XmlElement("Text")]
    required public string Text { get; set; }
}
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
