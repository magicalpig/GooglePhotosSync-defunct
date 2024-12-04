using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;

namespace VideoToolsCore.Metadata.Writers;

// provides  common functionality for metadata writers.
public abstract class MetadataWriterBase : IMetadataWriter
{
    public virtual int Priority { get; init; } = 1;
    
    public abstract HashSet<string> Capabilities { get; }

    // the metadata has at least one writable property WITH A VALUE
    public bool CanWriteAny(VideoMetadata metadata)
    {
        return Capabilities.Any(metadata.HasWritableValueForProperty);    
    }

    public abstract Task<List<string>> WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata metadata);
}