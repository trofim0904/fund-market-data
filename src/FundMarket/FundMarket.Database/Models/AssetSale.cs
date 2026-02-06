using System.ComponentModel.DataAnnotations;

namespace FundMarket.Database.Models;

public class AssetSale
{
    public Guid Id { get; init; }

    [MaxLength(10)]
    public string? Ticker { get; set; }

    public DateTime Date { get; set; }

    public decimal Price { get; set; }

    public decimal Qty { get; set; }
}