namespace VideoToolsCore.Models;

public class VideoFileWindows : VideoFileBase
{  
  public string Path { get; set; } = String.Empty;
  public string Folder { get; set; } = String.Empty;
  public string Codec { get; set; } = String.Empty;
  public DateTime? LastModified { get; set; }

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

  public bool LacksMetadata()
  {
    return CreatedDate is null 
      || DurationMs is null || DurationMs <= 0
      || Codec is null
      || string.IsNullOrEmpty(Description);
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
}