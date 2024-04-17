namespace Geodatenbezug.Models;

/// <summary>
/// Status of the geodata export.
/// </summary>
public enum GeodiensteStatus
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    Queued,
    Working,
    Success,
    Failed,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
