using System.ComponentModel.DataAnnotations.Schema;

namespace VideoToolsCore.Models;

[Table("WindowsVideos")]
public class VideoFileWindows : VideoFileBase
{
    public string Path { get; set; } = String.Empty;
    public string Folder { get; set; } = String.Empty;
    public string Codec { get; set; } = String.Empty;
    public DateTime? LastModified { get; set; }
    internal string MachineName { get; set; } = String.Empty;
    internal string MachineId { get; set; } = String.Empty;


    public void SyncTo(VideoFileWindows other)
    {
        base.SyncTo(other);
        Codec = other.Codec;
        LastModified = other.LastModified;
        // neither folder nor path should ever change
    }
    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        VideoFileWindows other = (VideoFileWindows)obj;

        return base.Equals(obj) &&
             Path == other.Path &&
             Folder == other.Folder &&
             Codec == other.Codec &&
             LastModified == other.LastModified;
    }

    // these are the metadata fields that all videos should have.
    // If this method returns true, you probably want to give another go at scraping the metadata.     
    public bool LacksStandardMetadata()
    {
        return CreatedDate is null
          || DurationMs is null || DurationMs <= 0
          || Codec is null;
    }

    // likely to used on an existing db record against one discovered during a filesystem scrape
    public bool ModifiedBefore(VideoFileWindows other)
    {
        return !LastModified.HasValue || other.LastModified.HasValue && LastModified < other.LastModified;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Path, Folder, Codec, LastModified);
    }

    public override void AttachMetadata(Metadata m)
    {
        base.AttachMetadata(m);
        Codec = m.Codec;
    }
}