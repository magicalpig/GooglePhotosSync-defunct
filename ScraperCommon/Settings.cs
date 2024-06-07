namespace ScraperCommon;
public class Settings
{
    public string? SourceDir { get; set; }
    public bool FreshDatabase { get; set; } = false;
    public string? FFMpegPath { get; set; }
    public int? FileLimitLocal { get; set; }
    public int? PageLimitGoogle { get; set; }
    public string? GoogleAPIClientId  { get; set; }
    public string? LocalUserId { get; set; }
    public bool ForceMetadataRefresh { get; set; } = false;
    public bool ScrapeGoogleVideos { get; set; } = true;
    public bool ScrapeGoogleAlbums { get; set; } = true;
}