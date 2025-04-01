using System.ComponentModel.DataAnnotations;

namespace FundMarket.Database.Models;

public class Ticker
{
    public Guid Id { get; set; }

    [MaxLength(10)]
    public string? Name { get; set; }
}