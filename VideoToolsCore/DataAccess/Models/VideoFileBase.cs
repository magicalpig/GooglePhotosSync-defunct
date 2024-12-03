using VideoToolsCore.Attributes;
using VideoToolsCore.Metadata.Models;

namespace VideoToolsCore.DataAccess.Models;

public abstract class VideoFileBase : OwnableEntityBase
{
    [FromGoogleRestApi]
    public string? Name { get; set; } = String.Empty;
    [FromGoogleRestApi]
    public string? Description { get; set; }
    public long? Size { get; set; }

    public DateTime? LastMetadataScanTimestamp { get; set; }

    // this does not get set directly. It gets set when you set the milliseconds version
    public TimeSpan? Duration { get; private set; }
    private long? _durationMs;
    public long? DurationMs
    {
        get { return _durationMs; }
        set
        {
            // setting DurationMs directly would result in infinite recursion
            if (value.HasValue)
            {
                long nonNullValue = value.Value;
                _durationMs = nonNullValue;
                Duration = TimeSpan.FromMilliseconds(nonNullValue - nonNullValue % 1000);
            }
        }
    }

    private DateTime? _createdDate;
    [FromGoogleRestApi]
    public DateTime? CreatedDate
    {
        get { return _createdDate; }
        // setter has extra functionality to remove milliseconds from the date
        set
        {
            if (value.HasValue)
            {
                DateTime d = value.Value;
                // clear milliseconds off date stamp
                d = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second, 0, d.Kind);
                _createdDate = d;
            }
            else
                _createdDate = null;
        }
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
        CreatedDate = other.CreatedDate;
        Description = other.Description;
        DurationMs = other.DurationMs;
        Size = other.Size;
        LastMetadataScanTimestamp = other.LastMetadataScanTimestamp;
    }

    public virtual void AttachMetadata(VideoMetadata m)
    {
        DurationMs = m.DurationMs;
        CreatedDate = m.CreatedDate;
        Description = m.Description;
        LastMetadataScanTimestamp = DateTime.Now;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, CreatedDate, Description, Duration, Size);
    }    
}