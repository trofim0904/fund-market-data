using System.ComponentModel.DataAnnotations;

namespace FundMarket.Database.Models;

public class Ticker
{
    public Guid Id { get; init; }

    [MaxLength(10)]
    public required string Name { get; init; }

    public bool? IsIgnored { get; set; }

    public decimal? ExpectedPercent { get; set; }

    public override string ToString() => $"Ticker: {Name,-5} Ignored: {IsIgnored ?? false} "
        + (ExpectedPercent is null ? string.Empty : $"Expected: {ExpectedPercent}%");
}