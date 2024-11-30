using VideoToolsCore.DataAccess;
using VideoToolsCore.Services;
using VideoToolsCore.Utilities;

namespace MetadataGoogleToWindows;

public class Settings : BaseSettings, IProvidesFFMpegPath, IProvidesDbFilePath
{
    public string? SourceDir { get; set; }    
    public string? FFMpegPath { get; set; }
    public string? DbFilePath { get; set; }
    public string? MetadataProperty { get; set; }
    public string? GoogleOwner { get; set; }
}