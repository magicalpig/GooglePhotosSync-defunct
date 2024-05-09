using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CsvHelper.Configuration;

namespace VidCommon.Models;
[Table("GoogleVideos")]
public class VideoFileGoogle : VideoFileBase
{ 
  [Key]
  public string GoogleId { get; set; } = String.Empty;
  public string? URL { get; set; }

  public override bool Equals(object? obj)
  {
    if (obj == null || GetType() != obj.GetType())
    {
      return false;
    }

    VideoFileGoogle other = (VideoFileGoogle)obj;

    return base.Equals(obj) &&
         GoogleId == other.GoogleId &&
         URL == other.URL;
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(base.GetHashCode(), GoogleId, URL);
  }

  public void SyncTo(VideoFileGoogle other)
  {
    base.SyncTo(other);
    URL = other.URL;
  }
}

// For CSV output
public sealed class VideoFileGoogleMap : ClassMap<VideoFileGoogle>
{
  public VideoFileGoogleMap()
  {
    Map(m => m.Name).Index(0).Name("Google Name");
    Map(m => m.CreatedDate).Index(1).Name("Google Date");
    Map(m => m.Description).Index(2).Name("Google Description");
    Map(m => m.Duration).Index(3).Name("Google Duration");
    Map(m => m.Duration).Index(4).Name("Google Duration ms");
    Map(m => m.Size).Index(5).Name("Google Size");
    Map(m => m.GoogleId).Index(6).Name("Google Id");
    Map(m => m.URL).Index(7).Name("Google URL");
  }
}