namespace VidCommon.Models;

public abstract class VideoFileBase
{
  public string? Name { get; set; } = String.Empty;
  private DateTime? _createdDate;
  public DateTime? CreatedDate
  {
    get { return _createdDate; }
    // setter has extra functionality to remove milliseconds from the date
    set
    {
      if (value.HasValue)
      {
        DateTime d = value.Value;
        long ms = d.Millisecond;
        if (ms > 0)
          d = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second, 0, d.Kind);
        _createdDate = d;
      }
      else
      {
        _createdDate = null;
      }
    }
  }
  public string? Description { get; set; } = String.Empty;
  // this does not get set directly. It gets set when you set the milliseconds version
  public TimeSpan Duration { get; private set; } = new TimeSpan(0);

  private long _durationMs;
  public long DurationMs
  {
    get { return _durationMs; }
    set
    {
      // setting DurationMs directly would result in infinite recursion
      _durationMs = value;
      Duration = TimeSpan.FromMilliseconds(value - value % 1000);
    }
  }
  public long Size { get; set; } = 0;
  public override bool Equals(object? obj)
  {
    if (obj == null || GetType() != obj.GetType())
    {
      return false;
    }

    VideoFileBase other = (VideoFileBase)obj;

    return Name == other.Name &&
         CreatedDate == other.CreatedDate &&
         Description == other.Description &&
         Duration == other.Duration &&
         DurationMs == other.DurationMs &&
         Size == other.Size;
  }

  protected void SyncTo(VideoFileBase other)
  {
    Name = other.Name;
    CreatedDate = other.CreatedDate;
    Description = other.Description;
    Duration = other.Duration;
    DurationMs = other.DurationMs;
    Size = other.Size;
  }
  public override int GetHashCode()
  {
    return HashCode.Combine(Name, CreatedDate, Description, Duration, Size);
  }
}