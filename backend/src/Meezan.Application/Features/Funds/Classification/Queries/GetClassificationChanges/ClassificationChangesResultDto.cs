using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationChanges;

public sealed class ClassificationChangesResultDto
{
    public List<FundClassificationChangeLog> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
