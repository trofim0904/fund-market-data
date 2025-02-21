using FundMarketLibrary.Model;

namespace FundMarketLibrary.Logic;

public interface IAssetReader
{
    /// <summary>
    /// Retrieves the asset price for the specified ticker.
    /// </summary>
    Asset GetAsset(string ticker);
    
    /// <summary>
    /// Retrieves the asset price for the specified ticker.
    /// </summary>
    Task<Asset> GetAssetAsync(string ticker);

    /// <summary>
    /// Retrieves the asset prices for a collection of specified tickers.
    /// </summary>
    IEnumerable<Asset> GetAssets(IEnumerable<string> tickers);

    /// <summary>
    /// Retrieves the asset prices for a collection of specified tickers.
    /// </summary>
    Task<IEnumerable<Asset>> GetAssetsAssync(IEnumerable<string> tickers);
}