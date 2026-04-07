using Archive.API.Models;
using System.Text.Json;

namespace Archive.API.Services;

public class JsonUserStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonUserStore(IHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "DataStore");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "users.json");
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalized = NormalizeEmail(email);
        var users = await ReadAllAsync();
        return users.FirstOrDefault(u => u.Email == normalized);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var users = await ReadAllAsync();
        return users.FirstOrDefault(u => u.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var normalized = NormalizeEmail(email);
        var users = await ReadAllAsync();
        return users.Any(u => u.Email == normalized);
    }

    public async Task AddAsync(User user)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAllInternalAsync();
            users.Add(user);
            var json = JsonSerializer.Serialize(users, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<List<WishlistEntry>> GetWishlistAsync(Guid userId)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAllInternalAsync();
            return users.FirstOrDefault(u => u.Id == userId)?.WishlistEntries.ToList() ?? [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> WishlistExistsAsync(Guid userId, Guid fashionItemId)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAllInternalAsync();
            var user = users.FirstOrDefault(u => u.Id == userId);
            return user?.WishlistEntries.Any(w => w.FashionItemId == fashionItemId) ?? false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<WishlistEntry?> AddWishlistAsync(Guid userId, Guid fashionItemId, string? note)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAllInternalAsync();
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user is null)
                return null;

            var entry = new WishlistEntry
            {
                UserId = userId,
                FashionItemId = fashionItemId,
                Note = note
            };

            user.WishlistEntries.Add(entry);
            await SaveAllInternalAsync(users);
            return entry;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> RemoveWishlistAsync(Guid userId, Guid entryId)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAllInternalAsync();
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user is null)
                return false;

            var entry = user.WishlistEntries.FirstOrDefault(w => w.Id == entryId);
            if (entry is null)
                return false;

            user.WishlistEntries.Remove(entry);
            await SaveAllInternalAsync(users);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<List<TrackedItem>> GetTrackingAsync(Guid userId)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAllInternalAsync();
            return users.FirstOrDefault(u => u.Id == userId)?.TrackedItems.ToList() ?? [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> TrackingExistsAsync(Guid userId, Guid fashionItemId)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAllInternalAsync();
            var user = users.FirstOrDefault(u => u.Id == userId);
            return user?.TrackedItems.Any(t => t.FashionItemId == fashionItemId) ?? false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<TrackedItem?> AddTrackingAsync(Guid userId, Guid fashionItemId, decimal? targetPrice)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAllInternalAsync();
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user is null)
                return null;

            var entry = new TrackedItem
            {
                UserId = userId,
                FashionItemId = fashionItemId,
                TargetPrice = targetPrice
            };

            user.TrackedItems.Add(entry);
            await SaveAllInternalAsync(users);
            return entry;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<TrackedItem?> UpdateTrackingTargetPriceAsync(Guid userId, Guid trackedId, decimal? targetPrice)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAllInternalAsync();
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user is null)
                return null;

            var tracked = user.TrackedItems.FirstOrDefault(t => t.Id == trackedId);
            if (tracked is null)
                return null;

            tracked.TargetPrice = targetPrice;
            await SaveAllInternalAsync(users);
            return tracked;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> RemoveTrackingAsync(Guid userId, Guid trackedId)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAllInternalAsync();
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user is null)
                return false;

            var tracked = user.TrackedItems.FirstOrDefault(t => t.Id == trackedId);
            if (tracked is null)
                return false;

            user.TrackedItems.Remove(tracked);
            await SaveAllInternalAsync(users);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<User>> ReadAllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return await ReadAllInternalAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<User>> ReadAllInternalAsync()
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(_filePath);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<User>>(json) ?? [];
    }

    private async Task SaveAllInternalAsync(List<User> users)
    {
        var json = JsonSerializer.Serialize(users, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
