using VideoToolsCore.DataAccess;
using VideoToolsCore.Utilities;

namespace WebHelper;

public class Settings : BaseSettings, IProvidesDbFilePath
{
    public string? DbFilePath { get; set; }
}