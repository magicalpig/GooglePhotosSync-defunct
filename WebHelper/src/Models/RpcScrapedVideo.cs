namespace WebHelper.Models;
public class RpcScrapedVideo(string rpcId, long size, long durationMs, string? googleId)
{
  public string RpcId {get; init; } = rpcId;
  public long Size {get; init; } = size;
  public long DurationMs {get; init; } = durationMs;
  public string? GoogleId {get; init; } = googleId;
}