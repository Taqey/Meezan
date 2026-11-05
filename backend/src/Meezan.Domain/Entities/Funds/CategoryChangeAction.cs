namespace Meezan.Domain.Entities;

public enum CategoryChangeAction
{
    AutoAdded = 0,
    AutoRemoved = 1,
    ManualIncluded = 2,
    ManualExcluded = 3,
    ManualIncludeRemoved = 4,
    ManualExcludeRemoved = 5,
    ResetToAuto = 6,
    CategoryCreated = 7,
    CategoryRenamed = 8,
    CategoryDeleted = 9,
    CategoryRestored = 10,
    CategoryMerged = 11
}
