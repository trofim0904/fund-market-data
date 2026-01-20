using FundMarket.Core.Models;

namespace FundMarket.Core;

public interface IStockDataRecommendationService
{
    /// <summary>
    /// Gets recommended assets to buy based on the specified amount.
    /// </summary>
    Task<IEnumerable<AssetRecommendation>> GetAssetRecommendationAsync(decimal amountToInvest);
}