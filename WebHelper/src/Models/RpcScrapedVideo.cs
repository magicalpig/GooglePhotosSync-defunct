namespace WebHelper.Models;
public record RpcScrapedVideo()
{
    public string? RpcId { get; init; }
    public long? Size { get; init; }
    public long? DurationMs { get; init; }
    public string? GoogleId { get; init; }
}