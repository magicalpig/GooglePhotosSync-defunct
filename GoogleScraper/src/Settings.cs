using VideoToolsCore.DataAccess;
using VideoToolsCore.Utilities;

namespace GoogleScraper;

public class Settings : BaseSettings, IProvidesDbFilePath
{
    public bool FreshDatabase { get; set; } = false;        
    public int? PageLimitGoogle { get; set; }
    public string? GoogleAPIClientId  { get; set; }
    public string? LocalUserId { get; set; }
    public bool ScrapeGoogleVideos { get; set; } = true;
    public bool ScrapeGoogleAlbums { get; set; } = true;
    public string? DbFilePath { get; set; }
    public bool FavoritesOnly { get; set; }
}