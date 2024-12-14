namespace WebHelper.Models;
public record WebScrapedVideo()
{
    public string? ProductId { get; init; }
    public long? Size { get; init; }
    public long? DurationMs { get; init; }
    public string? RestId { get; init; }
}