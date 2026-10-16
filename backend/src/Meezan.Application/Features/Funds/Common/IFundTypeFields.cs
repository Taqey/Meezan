using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Common;

/// <summary>Common shape for anything that carries the effective (possibly overridden) fund type.</summary>
public interface IFundTypeFields
{
    FundType Type { get; set; }
    string TypeNameAr { get; set; }
    string TypeNameEn { get; set; }
    int? TypeId { get; set; }
    string TypeSlug { get; set; }
    bool TypeIsOverridden { get; set; }
    /// <summary>Effective classification node slug (tree).</summary>
    string ClassificationSlug { get; set; }
    /// <summary>Full Arabic breadcrumb path, e.g. "صناديق أسهم › صناديق مؤشر › EGX33".</summary>
    string ClassificationPathAr { get; set; }
    /// <summary>Assignment source: Manual | Auto | SNDUK | None.</summary>
    string ClassificationSource { get; set; }
}
