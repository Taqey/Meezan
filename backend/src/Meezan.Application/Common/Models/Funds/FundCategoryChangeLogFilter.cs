using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Models.Funds;

public class FundCategoryChangeLogFilter
{
    public int? FundId { get; set; }
    public int? CategoryId { get; set; }
    public CategoryChangeAction? Action { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
