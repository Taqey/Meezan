namespace Meezan.Domain.Entities;

public enum FundTypeChangeAction
{
    Created = 0,
    Renamed = 1,
    Deleted = 2,
    Restored = 3,
    Merged = 4,
    Reordered = 5,
    VisibilityChanged = 6,
    FundOverridden = 7,
    FundOverrideRemoved = 8
}
