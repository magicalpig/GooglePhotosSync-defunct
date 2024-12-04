using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;

namespace VideoToolsCore.Metadata.Writers;

public interface IMetadataWriter : IPrioritizedMetadataStrategy
{
    Task<List<string>> WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata metadata);
    // These are the metadata properties that each writer can write. 
    // The values should match VideoMetadata property names.
    HashSet<string> Capabilities { get; }

    bool CanWriteAny(VideoMetadata metadata);
}