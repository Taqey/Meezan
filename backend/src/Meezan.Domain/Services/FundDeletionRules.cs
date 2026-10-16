namespace Meezan.Domain.Services;

public static class FundDeletionRules
{
    public static (bool CanDelete, string? Reason) CanDeleteNode(
        int nodeId,
        bool isProtectedOrSeed,
        int activeFundsCount,
        int childrenCount)
    {
        if (isProtectedOrSeed)
            return (false, "لا يمكن حذف عقدة أساسية من الشجرة.");

        if (childrenCount > 0)
            return (false, "لا يمكن حذف عقدة تحتوي على فروع فرعية. انقل الفروع أو احذفها أولاً.");

        if (activeFundsCount > 0)
            return (false, $"لا يمكن حذف العقدة لوجود {activeFundsCount} صندوق مرتبط بها حالياً.");

        return (true, null);
    }
}
