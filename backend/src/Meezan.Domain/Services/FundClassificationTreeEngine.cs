using Meezan.Domain.Entities;

namespace Meezan.Domain.Services;

public static class FundClassificationTreeEngine
{
    public const int MaxDepth = FundClassificationNode.MaxDepth;

    public static List<FundClassificationNode> BuildPath(FundClassificationNode node, IReadOnlyDictionary<int, FundClassificationNode> byId)
    {
        var path = new List<FundClassificationNode>();
        var cur = node;
        var guard = 0;
        while (cur != null && guard++ < 10)
        {
            path.Add(cur);
            cur = cur.ParentId.HasValue && byId.TryGetValue(cur.ParentId.Value, out var p) ? p : null;
        }
        path.Reverse();
        return path;
    }

    public static HashSet<int> DescendantIds(IEnumerable<FundClassificationNode> nodes, int nodeId)
    {
        var byParent = nodes.Where(n => n.ParentId.HasValue).GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(n => n.Id).ToList());
        var outSet = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(nodeId);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (!byParent.TryGetValue(cur, out var kids)) continue;
            foreach (var k in kids)
            {
                if (outSet.Add(k)) stack.Push(k);
            }
        }
        return outSet;
    }

    public static int SubtreeHeight(List<FundClassificationNode> all, int rootId)
    {
        var kids = all.Where(n => n.ParentId == rootId).ToList();
        if (kids.Count == 0) return 0;
        return 1 + kids.Max(k => SubtreeHeight(all, k.Id));
    }

    public static int ComputeLevel(FundClassificationNode node, IReadOnlyDictionary<int, FundClassificationNode> byId)
    {
        int level = 0;
        var cur = node;
        var guard = 0;
        while (cur.ParentId.HasValue && guard++ < 10)
        {
            if (!byId.TryGetValue(cur.ParentId.Value, out var p)) break;
            level++;
            cur = p;
        }
        return level;
    }

    public static (bool Ok, string? Error) ValidateMove(int nodeId, int? newParentId, List<FundClassificationNode> allNodes)
    {
        var byId = allNodes.ToDictionary(n => n.Id);
        if (!byId.TryGetValue(nodeId, out _)) return (false, "العقدة غير موجودة.");
        if (newParentId == null) return (true, null); // moving to root
        if (newParentId == nodeId) return (false, "لا يمكن نقل العقدة تحت نفسها.");
        if (!byId.TryGetValue(newParentId.Value, out var parent)) return (false, "الأب الجديد غير موجود.");

        // cycle check: new parent must not be a descendant of node
        var desc = DescendantIds(allNodes, nodeId);
        if (desc.Contains(newParentId.Value)) return (false, "لا يمكن نقل العقدة تحت إحدى بناتها (دورة).");

        // depth check: newLevel + subtree height must stay < MaxDepth
        int newLevel = parent.Level + 1;
        int height = SubtreeHeight(allNodes, nodeId);
        if (newLevel + height > MaxDepth - 1)
            return (false, $"النقل يتجاوز أقصى عمق ({MaxDepth} مستويات).");

        return (true, null);
    }

    public static string GenerateSlug(string input)
    {
        var s = (input ?? string.Empty).Trim().ToLowerInvariant()
            .Replace(" ", "-").Replace("_", "-").Replace(".", "").Replace("/", "-");
        var sb = new System.Text.StringBuilder();
        foreach (var ch in s)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_')
                sb.Append(ch);
        }
        return sb.ToString().Trim('-');
    }
}
