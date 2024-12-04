using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;

namespace VideoToolsCore.Metadata.Readers;

public interface IMetadataReader : IPrioritizedMetadataStrategy
{
    public Task<VideoMetadata> GetMetadataAsync(VideoFileWindows videoFile);
}