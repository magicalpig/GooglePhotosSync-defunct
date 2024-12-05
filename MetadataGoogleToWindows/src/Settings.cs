using VideoToolsCore.DataAccess;
using VideoToolsCore.Metadata.Models;
using VideoToolsCore.Utilities;

namespace MetadataGoogleToWindows;

public class Settings : BaseSettings, IProvidesFFMpegPath, IProvidesDbFilePath
{
    public string? SourceDir { get; set; }    
    public string? FFMpegPath { get; set; }
    public string? DbFilePath { get; set; }    
    public List<string>? MetadataProperties { get; set; } // these value should match VideoMetadata property names
    public string? GoogleOwner { get; set; }
}