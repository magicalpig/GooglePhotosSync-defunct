using VideoToolsCore.DataAccess;
using VideoToolsCore.Metadata.Models;
using VideoToolsCore.Utilities;

namespace MetadataGoogleToWindows;

public class Settings : BaseSettings, IProvidesFFMpegPath, IProvidesDbFilePath
{
    public string? SourceDir { get; set; }    
    public string? FFMpegPath { get; set; }
    public string? DbFilePath { get; set; }    
    public string? MetadataProperty { get; set; } // this should match a VideoMetadata property name
    public string? GoogleOwner { get; set; }
}