using Archive.API.DTOs;
using Archive.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Archive.API.Controllers;

// ── Wishlist ──────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/wishlist")]
[Authorize]
[Produces("application/json")]
public class WishlistController(JsonCatalogStore catalogStore, JsonUserStore userStore) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lista todos os itens da wishlist do usuário.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WishlistEntryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var items = await catalogStore.GetItemsAsync();
        var entries = (await userStore.GetWishlistAsync(UserId))
            .OrderByDescending(w => w.AddedAt)
            .Join(
                items,
                w => w.FashionItemId,
                i => i.Id,
                (w, i) => new WishlistEntryResponse(
                    w.Id,
                    w.FashionItemId,
                    i.Name,
                    i.Brand,
                    i.CurrentPrice,
                    i.ImageUrl,
                    w.AddedAt,
                    w.Note))
            .ToList();

        return Ok(entries);
    }

    /// <summary>Adiciona um item à wishlist.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WishlistEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add([FromBody] AddToWishlistRequest req)
    {
        var item = await catalogStore.GetItemByIdAsync(req.FashionItemId);
        if (item is null) return NotFound(new { error = "Item não encontrado." });

        if (await userStore.WishlistExistsAsync(UserId, req.FashionItemId))
            return Conflict(new { error = "Item já está na wishlist." });

        var entry = await userStore.AddWishlistAsync(UserId, req.FashionItemId, req.Note);
        if (entry is null) return Unauthorized();

        return CreatedAtAction(nameof(GetAll), new WishlistEntryResponse(
            entry.Id, item.Id, item.Name, item.Brand,
            item.CurrentPrice, item.ImageUrl, entry.AddedAt, entry.Note));
    }

    /// <summary>Remove um item da wishlist.</summary>
    [HttpDelete("{entryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid entryId)
    {
        if (!await userStore.RemoveWishlistAsync(UserId, entryId)) return NotFound();
        return NoContent();
    }
}

// ── Tracking ──────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/tracking")]
[Authorize]
[Produces("application/json")]
public class TrackingController(JsonCatalogStore catalogStore, JsonUserStore userStore) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lista todos os itens em tracking do usuário.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TrackedItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var fashionItems = await catalogStore.GetItemsAsync();
        var items = (await userStore.GetTrackingAsync(UserId))
            .OrderByDescending(t => t.AddedAt)
            .Join(
                fashionItems,
                t => t.FashionItemId,
                i => i.Id,
                (t, i) => new TrackedItemResponse(
                    t.Id,
                    t.FashionItemId,
                    i.Name,
                    i.Brand,
                    i.CurrentPrice,
                    t.TargetPrice,
                    i.ImageUrl,
                    t.AddedAt))
            .ToList();

        return Ok(items);
    }

    /// <summary>Adiciona um item ao tracking.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TrackedItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add([FromBody] AddToTrackingRequest req)
    {
        var item = await catalogStore.GetItemByIdAsync(req.FashionItemId);
        if (item is null) return NotFound(new { error = "Item não encontrado." });

        if (await userStore.TrackingExistsAsync(UserId, req.FashionItemId))
            return Conflict(new { error = "Item já está em tracking." });

        var tracked = await userStore.AddTrackingAsync(UserId, req.FashionItemId, req.TargetPrice);
        if (tracked is null) return Unauthorized();

        return CreatedAtAction(nameof(GetAll), new TrackedItemResponse(
            tracked.Id, item.Id, item.Name, item.Brand,
            item.CurrentPrice, tracked.TargetPrice, item.ImageUrl, tracked.AddedAt));
    }

    /// <summary>Atualiza o preço-alvo de um item em tracking.</summary>
    [HttpPatch("{trackedId:guid}/target-price")]
    [ProducesResponseType(typeof(TrackedItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTargetPrice(Guid trackedId, [FromBody] decimal? targetPrice)
    {
        var tracked = await userStore.UpdateTrackingTargetPriceAsync(UserId, trackedId, targetPrice);

        if (tracked is null) return NotFound();

        var item = await catalogStore.GetItemByIdAsync(tracked.FashionItemId);
        if (item is null) return NotFound();

        return Ok(new TrackedItemResponse(
            tracked.Id, tracked.FashionItemId, item.Name,
            item.Brand, item.CurrentPrice,
            tracked.TargetPrice, item.ImageUrl, tracked.AddedAt));
    }

    /// <summary>Remove um item do tracking.</summary>
    [HttpDelete("{trackedId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid trackedId)
    {
        if (!await userStore.RemoveTrackingAsync(UserId, trackedId)) return NotFound();
        return NoContent();
    }
}
