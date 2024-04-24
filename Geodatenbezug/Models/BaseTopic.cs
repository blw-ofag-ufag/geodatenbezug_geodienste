using System.ComponentModel;

namespace Geodatenbezug.Models;

/// <summary>
/// Represents the base topic names of the geodata.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Names come from geodienste.ch and cannot be changed.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Names come from geodienste.ch and cannot be changed.")]
public enum BaseTopic
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [Description("Perimeter Ln Sf")]
    lwb_perimeter_ln_sf,
    [Description("Rebbaukataster")]
    lwb_rebbaukataster,
    [Description("Perimeter Terrassenreben")]
    lwb_perimeter_terrassenreben,
    [Description("Biodiversitaetsfoerderflaechen")]
    lwb_biodiversitaetsfoerderflaechen,
    [Description("Bewirtschaftungseinheit")]
    lwb_bewirtschaftungseinheit,
    [Description("Nutzungsflaechen")]
    lwb_nutzungsflaechen,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
