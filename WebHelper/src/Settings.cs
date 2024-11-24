using VideoToolsCore.DataAccess;

namespace WebHelper;

public record Settings : IProvidesDbFilePath
{
    public string? DbFilePath { get; set; }
}