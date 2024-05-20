namespace VidProbe;
public class Settings
{
    public string? SourceDir { get; set; }
    public bool FreshDatabase { get; set; }
    public string? FFMpegPath { get; set; }
    public int? FileLimitLocal { get; set; }
    public int? PageLimitGoogle { get; set; }
    public string? GoogleAPIClientId  { get; set; }
}