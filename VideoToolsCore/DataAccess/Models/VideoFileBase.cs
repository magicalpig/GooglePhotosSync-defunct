using System.ComponentModel.DataAnnotations.Schema;
using VideoToolsCore.Attributes;
using VideoToolsCore.Metadata.Models;

namespace VideoToolsCore.DataAccess.Models;

public abstract class VideoFileBase : OwnableEntityBase
{
    [FromGoogleRestApi]
    public string? Name { get; set; } = String.Empty;
    public long? Size { get; set; }
    public DateTime? LastMetadataScanTimestamp { get; set; }

    // Video files contain a VideoMetadata object
    // The VideoMetadata object is not itself stored in the db, but rather its properties
    // are stored alongside the VideoFileBase properties
    [NotMapped]
    protected VideoMetadata _metadata = new();

    [FromGoogleRestApi]
    public string? Description
    {
        get => _metadata.Description;
        set => _metadata.Description = value;
    }


    // The getter derives the value from the DurationMs property
    // The setter sets the DurationMs property to ensure Duration and DurationMs are in sync
    public TimeSpan? Duration
    {
        get => _metadata.DurationMs.HasValue ?
                TimeSpan.FromMilliseconds(_metadata.DurationMs.Value - _metadata.DurationMs.Value % 1000)
                : null;
        set => _metadata.DurationMs = (long?)(value?.TotalMilliseconds);
    }

    public long? DurationMs
    {
        get => _metadata.DurationMs;
        set => _metadata.DurationMs = value;        
    }

    [FromGoogleRestApi]
    public DateTime? CreatedDate
    {
        get => _metadata.CreatedDate;
        set => _metadata.CreatedDate = value;
    }


    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        VideoFileBase other = (VideoFileBase)obj;
        return
          Owner == other.Owner &&
          Name == other.Name &&
          CreatedDate == other.CreatedDate &&
          Description == other.Description &&
          Duration == other.Duration &&
          DurationMs == other.DurationMs &&
          Size == other.Size &&
          LastMetadataScanTimestamp == other.LastMetadataScanTimestamp;
    }

    protected void SyncTo(VideoFileBase other)
    {
        Name = other.Name;
        Size = other.Size;
        LastMetadataScanTimestamp = other.LastMetadataScanTimestamp;

        _metadata.Description = other._metadata.Description;
        _metadata.DurationMs = other._metadata.DurationMs;
        _metadata.CreatedDate = other._metadata.CreatedDate;
    }    

    public virtual void AttachMetadata(VideoMetadata m)
    {
        _metadata = m;
        LastMetadataScanTimestamp = DateTime.Now;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, CreatedDate, Description, Duration, Size);
    }
}