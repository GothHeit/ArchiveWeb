namespace Archive.API.DTOs;

public record AddToTrackingRequest(Guid FashionItemId, decimal? TargetPrice = null);
