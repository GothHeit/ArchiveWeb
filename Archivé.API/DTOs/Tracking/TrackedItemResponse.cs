namespace Archive.API.DTOs;

public record TrackedItemResponse(
    Guid Id,
    Guid FashionItemId,
    string ItemName,
    string Brand,
    decimal CurrentPrice,
    decimal? TargetPrice,
    string? ImageUrl,
    DateTime AddedAt
);
