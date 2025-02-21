namespace FundMarketLibrary.Descriptor;

public class Constant
{
    public static readonly Dictionary<char, decimal> LargeNumberRepresentation = new()
    {
        {
            'K', 1000
        },
        {
            'M', 1000000
        },
        {
            'B', 1000000000
        },
        {
            'T', 1000000000000
        },
    };
}