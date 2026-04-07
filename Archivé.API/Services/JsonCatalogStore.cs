using Archive.API.Models;
using System.Text.Json;

namespace Archive.API.Services;

public class JsonCatalogStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonCatalogStore(IHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "DataStore");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "catalog.json");
    }

    public async Task<List<FashionItem>> GetItemsAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadDataInternalAsync();
            return data.FashionItems;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<FashionItem?> GetItemByIdAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadDataInternalAsync();
            return data.FashionItems.FirstOrDefault(x => x.Id == id);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> ItemExistsAsync(Guid itemId)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadDataInternalAsync();
            return data.FashionItems.Any(x => x.Id == itemId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddItemWithInitialPriceAsync(FashionItem item)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadDataInternalAsync();
            data.FashionItems.Add(item);
            data.PriceHistories.Add(new PriceHistory
            {
                FashionItemId = item.Id,
                Price = item.CurrentPrice,
                Currency = item.Currency,
                Source = "manual"
            });
            await SaveDataInternalAsync(data);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<List<PriceHistory>> GetPriceHistoryAsync(Guid itemId)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadDataInternalAsync();
            return data.PriceHistories.Where(x => x.FashionItemId == itemId).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<PriceHistory?> AddPriceAsync(Guid itemId, decimal price, string currency, string? source)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadDataInternalAsync();
            var item = data.FashionItems.FirstOrDefault(x => x.Id == itemId);
            if (item is null)
                return null;

            var entry = new PriceHistory
            {
                FashionItemId = itemId,
                Price = price,
                Currency = currency,
                Source = source
            };

            data.PriceHistories.Add(entry);
            item.CurrentPrice = price;
            item.Currency = currency;
            item.UpdatedAt = DateTime.UtcNow;

            await SaveDataInternalAsync(data);
            return entry;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<CatalogData> ReadDataInternalAsync()
    {
        if (!File.Exists(_filePath))
            return new CatalogData();

        var json = await File.ReadAllTextAsync(_filePath);
        if (string.IsNullOrWhiteSpace(json))
            return new CatalogData();

        return JsonSerializer.Deserialize<CatalogData>(json) ?? new CatalogData();
    }

    private async Task SaveDataInternalAsync(CatalogData data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    private class CatalogData
    {
        public List<FashionItem> FashionItems { get; set; } = [];
        public List<PriceHistory> PriceHistories { get; set; } = [];
    }
}
