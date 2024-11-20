using VideoToolsCore.DataAccess;
using VideoToolsCore.Services;

namespace LocalScraper;

public record Settings : IProvidesFFMpegPath, IProvidesDbFilePath
{
    public string? SourceDir { get; set; }
    public bool FreshDatabase { get; set; } = false;
    public string? FFMpegPath { get; set; }
    public int? FileLimit { get; set; }
    public bool ForceMetadataRefresh { get; set; } = false;
    public string? DbFilePath { get; set; }
}