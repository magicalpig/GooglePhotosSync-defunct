using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;

namespace VideoToolsCore.Metadata.Writers;

// provides  common functionality for metadata writers.
public abstract class MetadataWriterBase : IMetadataWriter
{
    public virtual int Priority { get; init; } = 1;
    
    public abstract HashSet<string> Capabilities { get; }

    public bool CanWriteAny(VideoMetadata metadata)
    {
        if (metadata.HasAnyWritableValues())
        {
            return metadata.GetWritableProperties().Any(Capabilities.Contains);
        }
        return false;
    }

    public abstract Task<List<string>> WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata metadata);
}