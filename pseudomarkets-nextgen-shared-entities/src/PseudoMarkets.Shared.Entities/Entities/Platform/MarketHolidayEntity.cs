namespace PseudoMarkets.Shared.Entities.Entities.Platform;

public class MarketHolidayEntity
{
    public DateOnly HolidayDate { get; set; }
    public string HolidayName { get; set; } = string.Empty;
}
