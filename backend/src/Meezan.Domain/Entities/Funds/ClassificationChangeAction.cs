namespace Meezan.Domain.Entities;

public enum ClassificationChangeAction
{
    AutoAdded = 0,
    AutoRemoved = 1,
    ManualIncluded = 2,
    ManualExcluded = 3,
    ManualIncludeRemoved = 4,
    ManualExcludeRemoved = 5,
    ResetToAuto = 6,
    NodeCreated = 7,
    NodeRenamed = 8,
    NodeDeleted = 9,
    NodeRestored = 10,
    NodeMerged = 11,
    FundAssigned = 12,
    FundRemoved = 13,
    FundOverridden = 14,
    FundOverrideRemoved = 15,
    NodeMoved = 16,
    NodeReordered = 17,
    VisibilityChanged = 18,
    RuleChanged = 19,
    BulkAssigned = 20,
    BulkRemoved = 21,
    Reclassified = 22
}
