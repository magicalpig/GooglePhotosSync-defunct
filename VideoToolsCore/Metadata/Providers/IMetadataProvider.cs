using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;

namespace VideoToolsCore.Metadata.Providers;

public interface IMetadataProvider
{
    public Task<Models.VideoMetadata> GetMetadataAsync(VideoFileWindows videoFile);
    public Task WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata metadata);
}