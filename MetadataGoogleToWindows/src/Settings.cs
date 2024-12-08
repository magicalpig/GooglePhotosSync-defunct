using VideoToolsCore.DataAccess;
using VideoToolsCore.Metadata.Models;
using VideoToolsCore.Utilities;

namespace MetadataGoogleToWindows;

public class Settings : BaseSettings, IProvidesFFMpegPath, IProvidesDbFilePath
{
    private string? _sourceDir;
    public string? SourceDir
    {
        get => NormalizePath(_sourceDir);
        set => _sourceDir = value;
    }
    public string? FFMpegPath { get; set; }
    public string? DbFilePath { get; set; }
    public List<string>? MetadataProperties { get; set; } // these value should match VideoMetadata property names
    public string? GoogleOwner { get; set; }
}