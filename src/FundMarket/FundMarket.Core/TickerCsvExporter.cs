using System.Globalization;
using System.Text;
using FundMarket.Core.Models;

namespace FundMarket.Core;

public class TickerCsvExporter
{
    public string CreateCsv(IEnumerable<TickerExportItem> tickers)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Name,IsIgnored,ExpectedPercent,CurrentPrice");
        foreach (var ticker in tickers)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(ticker.Name),
                ticker.IsIgnored.ToString(),
                ticker.ExpectedPercent.ToString(CultureInfo.InvariantCulture),
                ticker.CurrentPrice.ToString(CultureInfo.InvariantCulture)));
        }
        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
