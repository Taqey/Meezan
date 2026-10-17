namespace Meezan.WebApi.Contracts.AdminStocksManagement;

public sealed record AddToIndexRequest(
    decimal? Weight,
    DateTime? EffectiveDate);
