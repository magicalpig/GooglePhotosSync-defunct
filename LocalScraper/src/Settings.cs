using VideoToolsCore.DataAccess;
using VideoToolsCore.Metadata.Services;
using VideoToolsCore.Utilities;

namespace LocalScraper;

public class Settings : BaseSettings, IProvidesFFMpegPath, IProvidesDbFilePath
{
    private string? _sourceDir;
    public string? SourceDir 
    { 
        get => NormalizePath(_sourceDir); 
        set => _sourceDir = value;
    }
    public bool FreshDatabase { get; set; } = false;
    public string? FFMpegPath { get; set; }
    public int? FileLimit { get; set; }
    public bool ForceMetadataRefresh { get; set; } = false;
    public string? DbFilePath { get; set; }
}