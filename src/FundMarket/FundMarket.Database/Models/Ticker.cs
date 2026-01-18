using System.ComponentModel.DataAnnotations;

namespace FundMarket.Database.Models;

public class Ticker
{
    public Guid Id { get; init; }

    [MaxLength(10)]
    public required string Name { get; init; }

    public bool? IsIgnored { get; set; }

    public decimal? ExpectedPercent { get; set; }
}