using VideoToolsCore.Services;

namespace LocalScraper;

public record Settings : IProvidesFFMpegPath
{
    public string? SourceDir { get; set; }
    public bool FreshDatabase { get; set; } = false;
    public string? FFMpegPath { get; set; }
    public int? FileLimitLocal { get; set; }
    public string? LocalUserId { get; set; }
    public bool ForceMetadataRefresh { get; set; } = false;
}