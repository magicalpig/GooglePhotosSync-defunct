public class Settings
{
    public bool FreshDatabase { get; set; } = false;        
    public int? PageLimitGoogle { get; set; }
    public string? GoogleAPIClientId  { get; set; }
    public string? LocalUserId { get; set; }
    public bool ScrapeGoogleVideos { get; set; } = true;
    public bool ScrapeGoogleAlbums { get; set; } = true;
}