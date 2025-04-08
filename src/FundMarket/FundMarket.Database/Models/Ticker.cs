using System.ComponentModel.DataAnnotations;

namespace FundMarket.Database.Models;

public class Ticker
{
    public Guid Id { get; init; }

    [MaxLength(10)]
    public string? Name { get; init; }

    public override string ToString() => $"Ticker: {Name,-5}";
}