using System.Xml.Serialization;

namespace Geodatenbezug.Models;

[XmlRoot("TRANSFER", Namespace = "http://www.interlis.ch/INTERLIS2.3")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
public class Transfer
{
    [XmlElement("DATASECTION")]
    public required DataSection DataSection { get; set; }
}

public class DataSection
{
    [XmlElement("LWB_Nutzungsflaechen_V2_0.LNF_Kataloge")]
    public required LnfKataloge LnfKataloge { get; set; }
}

public class LnfKataloge
{
    [XmlElement("LWB_Nutzungsflaechen_V2_0.LNF_Kataloge.LNF_Katalog_Nutzungsart")]
    public required List<LnfKatalogNutzungsart> LnfKatalogNutzungsart { get; set; }
}

public class LnfKatalogNutzungsart
{
    [XmlElement("LNF_Code")]
    public required int LnfCode { get; set; }

    [XmlElement("Nutzung")]
    public required Nutzung Nutzung { get; set; }

    [XmlElement("Hauptkategorie")]
    public required Hauptkategorie Hauptkategorie { get; set; }

    [XmlElement("Ist_BFF_QI")]
    public required bool IstBFFQI { get; set; }
}

public class Nutzung
{
    [XmlElement("LocalisationCH_V1.MultilingualText")]
    public required LocalisationCHV1MultilingualText LocalisationCHV1MultilingualText { get; set; }
}

public class Hauptkategorie
{
    [XmlElement("LocalisationCH_V1.MultilingualText")]
    public required LocalisationCHV1MultilingualText LocalisationCHV1MultilingualText { get; set; }
}

public class LocalisationCHV1MultilingualText
{
    [XmlElement("LocalisedText")]
    public required LocalisedText LocalisedText { get; set; }
}

public class LocalisedText
{
    [XmlElement("LocalisationCH_V1.LocalisedText")]
    public required List<LocalisationCHV1LocalisedText> LocalisationCHV1LocalisedText { get; set; }
}

public class LocalisationCHV1LocalisedText
{
    [XmlElement("Language")]
    public required string Language { get; set; }

    [XmlElement("Text")]
    public required string Text { get; set; }
}
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
