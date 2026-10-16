namespace Meezan.Application.Common.Interfaces;

/// <summary>
/// Marker interface to opt a MediatR command into automatic database transaction management via TransactionBehavior.
/// Note: Never apply this to long-running commands (e.g. scraping, Shariah refresh, background sync).
/// </summary>
public interface ITransactionalCommand
{
}
