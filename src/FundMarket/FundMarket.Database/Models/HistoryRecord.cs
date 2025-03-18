using System.ComponentModel.DataAnnotations;

namespace FundMarket.Database.Models;

public class HistoryRecord
{
    public Guid Id { get; set; }

    [MaxLength(10)]
    public string? Ticker { get; set; }

    public DateTime Date { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal FuturePrice { get; set; }

    public decimal MarketCap { get; set; }

    public decimal Change { get; set; }
}