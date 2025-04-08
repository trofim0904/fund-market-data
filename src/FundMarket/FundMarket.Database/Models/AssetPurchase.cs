using System.ComponentModel.DataAnnotations;

namespace FundMarket.Database.Models;

public class AssetPurchase
{
    public Guid Id { get; init; }

    [MaxLength(10)]
    public string? Ticker { get; init; }

    public DateTime Date { get; init; }

    public decimal Price { get; init; }

    public decimal Qty { get; init; }
}