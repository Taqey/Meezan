namespace Meezan.Domain.Entities;

/// <summary>
/// Fund type enum matching SNDUK types.
/// </summary>
public enum FundType
{
    Unknown = 0,
    Balanced = 1,
    CapitalPreservation = 2,
    Commodities = 3,
    Equity = 4,
    FixedIncome = 5,
    FixedIncomeUsd = 6,
    Gold = 7,
    Index = 8,
    MoneyMarket = 9
}
