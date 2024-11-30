using Microsoft.Extensions.Logging;
using VideoToolsCore.DataAccess;
using VideoToolsCore.Services;

namespace MetadataGoogleToWindows.Services;

public class MetadataCopier(VideoContext videoContext, ILogger<MetadataCopier> logger, Settings settings)
{
    private readonly VideoContext _videoContext = videoContext;
    private readonly ILogger<MetadataCopier> _logger = logger;
    private readonly Settings _settings = settings;    

    public async Task CopyMetadata(string sourceDir, string propToCopy, string googleOwner)
    {
         _logger.LogInformation("Copying metadata from Google videos to Windows videos. Path is {sourceDir} and property is {prop}", sourceDir, propToCopy);
        await Task.CompletedTask;
    }
}