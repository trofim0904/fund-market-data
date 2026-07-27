using FundMarket.Core.Extension;
using FundMarket.Core.Models;
using FundMarket.Database;
using FundMarket.Database.Models;
using FundMarket.Mapper;
using FundMarket.Reader.Logic;

namespace FundMarket.Core;
 
public class StockDataService(IUnitOfWork unitOfWork, IAssetReader reader) : IStockService
{
    public async Task AddTickerAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }
        var ticker = name.Trim().ToUpper();
        if (!unitOfWork.TickerRepository.Get(t => t.Name == ticker).Any())
        {
            var asset = await reader.GetAssetAsync(ticker);
            unitOfWork.TickerRepository.Insert(DBModelMapper.MapTickerRecord(asset));
            await unitOfWork.SaveChangesAsync();
        }
    }

    public async Task DeleteTickerAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }
        var ticker = name.Trim().ToUpper();
        var tickerToDelete = unitOfWork.TickerRepository.Get(t => t.Name == ticker).FirstOrDefault();
        if (tickerToDelete != null)
        {
            var tickerPurchases = unitOfWork.PurchaseRepository.Get(p => p.Ticker == ticker);
            var tickerSales = unitOfWork.SaleRepository.Get(s => s.Ticker == ticker);
            unitOfWork.PurchaseRepository.DeleteRange(tickerPurchases);
            unitOfWork.SaleRepository.DeleteRange(tickerSales);
            unitOfWork.TickerRepository.Delete(tickerToDelete);
            await unitOfWork.SaveChangesAsync();
        }
        else
        {
            throw new Exception("Ticker not found.");
        }
    }

    public async Task UpdateTickerAsync(string name, decimal percent, bool isIgnored)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }
        var ticker = name.Trim().ToUpper();
        var tickerToUpdate = unitOfWork.TickerRepository.Get(t => t.Name == ticker).FirstOrDefault();
        if (tickerToUpdate != null)
        {
            tickerToUpdate.ExpectedPercent = percent;
            tickerToUpdate.IsIgnored = isIgnored;
            unitOfWork.TickerRepository.Update(tickerToUpdate);
            await unitOfWork.SaveChangesAsync();
        }
        else
        {
            throw new Exception("Ticker not found.");
        }
    }

    public async Task BuyAssetAsync(string name, decimal buyOrderQty, decimal buyOrderPrice, DateTime buyOrderDate)
    {
        if (unitOfWork.TickerRepository.Get(t => t.Name == name).Any())
        {
            unitOfWork.PurchaseRepository.Insert(DBModelMapper.MapPurchase(name, buyOrderQty, buyOrderPrice,
                buyOrderDate));
            await unitOfWork.SaveChangesAsync();
        }
        else
        {
            throw new Exception("Ticker not found.");
        }
    }

    public async Task DeleteBuyAssetOrder(Guid id)
    {
        var order = unitOfWork.PurchaseRepository.Get(p => p.Id == id).First();
        unitOfWork.PurchaseRepository.Delete(order);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateBuyAssetOrder(Guid id, string name, decimal qty, decimal price, DateTime date)
    {
        var order = unitOfWork.PurchaseRepository.Get(p => p.Id == id).First();
        order.Ticker = name;
        order.Date = date;
        order.Price = price;
        order.Qty = qty;
        unitOfWork.PurchaseRepository.Update(order);
        await unitOfWork.SaveChangesAsync();
    }

    public Task<IEnumerable<AssetPurchase>> GetAllBuyOrders()
    {
        return Task.FromResult(unitOfWork.PurchaseRepository.Get());
    }

    public async Task SellAssetAsync(string name, decimal sellOrderQty, decimal sellOrderPrice, DateTime sellOrderDate)
    {
        if (unitOfWork.TickerRepository.Get(t => t.Name == name).Any())
        {
            unitOfWork.SaleRepository.Insert(DBModelMapper.MapSale(name, sellOrderQty, sellOrderPrice, sellOrderDate));
            await unitOfWork.SaveChangesAsync();
        }
        else
        {
            throw new Exception("Ticker not found.");
        }
    }

    public async Task DeleteSellAssetOrder(Guid id) 
    {
        var order = unitOfWork.SaleRepository.Get(p => p.Id == id).First();
        unitOfWork.SaleRepository.Delete(order);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateSellAssetOrder(Guid id, string name, decimal qty, decimal price, DateTime date)
    {
        var order = unitOfWork.SaleRepository.Get(p => p.Id == id).First();
        order.Ticker = name;
        order.Date = date;
        order.Price = price;
        order.Qty = qty;
        unitOfWork.SaleRepository.Update(order);
        await unitOfWork.SaveChangesAsync();
    }

    public Task<IEnumerable<AssetSale>> GetAllSellOrders()
    {
        return Task.FromResult(unitOfWork.SaleRepository.Get());
    }

    public Task<IEnumerable<Ticker>> GetAllTickersAsync()
    {
        return Task.FromResult<IEnumerable<Ticker>>(unitOfWork.TickerRepository.Get()
            .OrderBy(t => t.IsIgnored)
            .ThenByDescending(t => t.ExpectedPercent)
            .ThenBy(t => t.Name));
    }

    public async Task<decimal> GetCurrentPriceAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exception("Ticker not found.");
        }
        var ticker = name.Trim().ToUpper();
        var tickerExists = unitOfWork.TickerRepository.Get(t => t.Name == ticker).Any();
        if (!tickerExists)
        {
            throw new Exception("Ticker not found.");
        }
        var asset = await reader.GetAssetAsync(ticker);
        return asset.CurrentPrice;
    }

    public Task<AssetsSummary> GetBoughtAssetsAsync()
    {
        var tickers = unitOfWork.TickerRepository.Get(t => t.IsIgnored != true).GetWithExpectedPercent();
        var tickerNames = tickers.Select(t => t.Name);
        var purchases = unitOfWork.PurchaseRepository.Get(p => tickerNames.Contains(p.Ticker)).ToList();
        return GetBoughtAssetsAsync(tickers, tickerNames, purchases);
    }

    private async Task<AssetsSummary> GetBoughtAssetsAsync(List<Ticker> tickers, IEnumerable<string> tickerNames,
        List<AssetPurchase> purchases)
    {
        if (purchases.Count == 0)
        {
            return new AssetsSummary();
        }
        var sales = unitOfWork.SaleRepository.Get(p => tickerNames.Contains(p.Ticker)).ToList();
        var assets = await reader.GetAssetsAsync(tickers.Select(t => t.Name));
        var assetSummaries = Utilities.CreateAssetSummary(purchases, sales, tickers, assets.ToList());
        return GetBoughtAssetsAsync(assetSummaries);
    }

    private AssetsSummary GetBoughtAssetsAsync(IEnumerable<AssetSummaryItem> assetSummaries)
    {
        var result = new AssetsSummary();
        var items = assetSummaries.ToList();
        var pnl = decimal.Zero;
        var total = decimal.Zero;
        AssetSummaryItem? best = null;
        AssetSummaryItem? worst = null;
        foreach (var summary in items.OrderByDescending(s => s.Difference))
        {
            pnl += summary.Difference ?? decimal.Zero;
            total += summary.CurrentValue ?? decimal.Zero;
            best ??= summary;
            worst ??= summary;
            if (summary.Difference > best.Difference)
            {
                best = summary;
            }
            if (summary.Difference < worst.Difference)
            {
                worst = summary;
            }
            result.Assets.Add(summary);
        }
        result.TotalValue = Math.Round(total, 2, MidpointRounding.ToEven);
        result.TotalPnL = Math.Round(pnl, 2, MidpointRounding.ToEven);
        result.BestAsset = best?.Ticker;
        result.WorstAsset = worst?.Ticker;
        return result;
    }
}
