using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CsvHelper.Configuration;
namespace VidCommon.Models;

[Table("WindowsVideos")]
public class VideoFileWindows : VideoFileBase
{  
  [Key]
  public string Path { get; set; } = String.Empty;
  public string Folder { get; set; } = String.Empty;
  public void SyncTo(VideoFileWindows other)
  {
    base.SyncTo(other);
    // neither folder nor path should ever change, so it's okay to just update base class properties
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
         Folder == other.Folder;
  }
  public override int GetHashCode()
  {
    return HashCode.Combine(base.GetHashCode(), Path, Folder);
  }
}

// For CSV output
public sealed class VideoFileWindowsMap : ClassMap<VideoFileWindows>
{
  public VideoFileWindowsMap()
  {
    Map(m => m.Name).Index(0).Name("Windows Name");
    Map(m => m.CreatedDate).Index(1).Name("Windows Date");
    Map(m => m.Description).Index(2).Name("Windows Description");
    Map(m => m.Duration).Index(3).Name("Windows Duration");
    Map(m => m.Duration).Index(4).Name("Windows Duration ms");
    Map(m => m.Size).Index(5).Name("Windows Size");
    Map(m => m.Path).Index(6).Name("Windows Path");
    Map(m => m.Folder).Index(7).Name("Windows Folder");
  }
}