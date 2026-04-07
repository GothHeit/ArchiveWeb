using Archive.API.DTOs;
using Archive.API.Models;
using Archive.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archive.API.Controllers;

[ApiController]
[Route("api/items")]
[Produces("application/json")]
public class ItemsController(JsonCatalogStore store) : ControllerBase
{
    /// <summary>Busca itens de moda com filtros opcionais.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FashionItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] SearchItemsRequest req)
    {
        var query = (await store.GetItemsAsync()).AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Query))
        {
            var term = req.Query.ToLower();
            query = query.Where(f =>
                f.Name.ToLower().Contains(term) ||
                f.Brand.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(req.Brand))
            query = query.Where(f => f.Brand.ToLower() == req.Brand.ToLower());

        if (!string.IsNullOrWhiteSpace(req.Category))
            query = query.Where(f => f.Category != null && f.Category.ToLower() == req.Category.ToLower());

        if (req.MinPrice.HasValue)
            query = query.Where(f => f.CurrentPrice >= req.MinPrice.Value);

        if (req.MaxPrice.HasValue)
            query = query.Where(f => f.CurrentPrice <= req.MaxPrice.Value);

        var total = query.Count();
        var page = Math.Max(1, req.Page);
        var pageSize = Math.Clamp(req.PageSize, 1, 100);

        var items = query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => ToResponse(f))
            .ToList();

        return Ok(new PagedResult<FashionItemResponse>(items, total, page, pageSize));
    }

    /// <summary>Busca um item específico pelo ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FashionItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await store.GetItemByIdAsync(id);
        return item is null ? NotFound() : Ok(ToResponse(item));
    }

    /// <summary>Cadastra um novo item de moda.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(FashionItemResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateFashionItemRequest req)
    {
        var item = new FashionItem
        {
            Name = req.Name.Trim(),
            Brand = req.Brand.Trim(),
            Category = req.Category?.Trim(),
            ImageUrl = req.ImageUrl,
            ProductUrl = req.ProductUrl,
            CurrentPrice = req.CurrentPrice,
            Currency = req.Currency
        };

        await store.AddItemWithInitialPriceAsync(item);

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(item));
    }

    /// <summary>Retorna o histórico de preços de um item.</summary>
    [HttpGet("{id:guid}/price-history")]
    [ProducesResponseType(typeof(IEnumerable<PriceHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPriceHistory(
        Guid id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        if (!await store.ItemExistsAsync(id))
            return NotFound();

        var query = (await store.GetPriceHistoryAsync(id)).AsQueryable();

        if (from.HasValue)
            query = query.Where(p => p.RecordedAt >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(p => p.RecordedAt <= to.Value.ToUniversalTime());

        var history = query
            .OrderBy(p => p.RecordedAt)
            .Select(p => new PriceHistoryResponse(p.Id, p.Price, p.Currency, p.RecordedAt, p.Source))
            .ToList();

        return Ok(history);
    }

    /// <summary>Adiciona um novo registro de preço para um item.</summary>
    [HttpPost("{id:guid}/price-history")]
    [Authorize]
    [ProducesResponseType(typeof(PriceHistoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPrice(Guid id, [FromBody] AddPriceRequest req)
    {
        var entry = await store.AddPriceAsync(id, req.Price, req.Currency, req.Source);
        if (entry is null) return NotFound();

        return CreatedAtAction(
            nameof(GetPriceHistory),
            new { id },
            new PriceHistoryResponse(entry.Id, entry.Price, entry.Currency, entry.RecordedAt, entry.Source));
    }

    private static FashionItemResponse ToResponse(FashionItem f) =>
        new(f.Id, f.Name, f.Brand, f.Category, f.ImageUrl, f.ProductUrl,
            f.CurrentPrice, f.Currency, f.UpdatedAt);
}
