namespace Archive.API.Models;

public class TrackedItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid FashionItemId { get; set; }
    public decimal? TargetPrice { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public FashionItem FashionItem { get; set; } = null!;
}