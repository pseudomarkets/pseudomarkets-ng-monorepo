namespace PseudoMarkets.TransactionProcessing.Core.Interfaces;

public interface IMarketCalendarService
{
    DateOnly GetTradeDate(DateTime executedAtUtc);

    Task<DateOnly> GetSettlementDateAsync(DateOnly tradeDate, CancellationToken cancellationToken = default);

    Task<bool> IsMarketBusinessDayAsync(DateOnly date, CancellationToken cancellationToken = default);
}
