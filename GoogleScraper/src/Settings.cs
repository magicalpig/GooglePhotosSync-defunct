using VideoToolsCore.DataAccess;

public record Settings : IProvidesDbFilePath
{
    public bool FreshDatabase { get; set; } = false;        
    public int? PageLimitGoogle { get; set; }
    public string? GoogleAPIClientId  { get; set; }
    public string? LocalUserId { get; set; }
    public bool ScrapeGoogleVideos { get; set; } = true;
    public bool ScrapeGoogleAlbums { get; set; } = true;
    public string? DbFilePath { get; set; }
}