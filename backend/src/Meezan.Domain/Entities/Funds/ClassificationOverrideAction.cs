namespace Meezan.Domain.Entities;

/// <summary>
/// Action for a manual classification override.
/// </summary>
public enum ClassificationOverrideAction
{
    Include = 1,
    Exclude = 2,
    ResetToAuto = 3
}
