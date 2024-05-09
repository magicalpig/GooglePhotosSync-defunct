using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace VidCommon.Models;
  
[Table("GoogleVideosMetadataFromChrome")]
public class VideoMetadataFromChrome
{
  [Key]
  [ForeignKey("VideoFileGoogle")]
  public string? ProductId { get; set; }
  public int? Filesize { get; set; }
  public string? Filename { get; set; }
  public string? RestId { get; set; }
  public int? DurationMs { get; set; }
  public override string ToString()
  {
    return $"Id: {ProductId}, Size: {Filesize}, Filename: {Filename}, RestId: \"{RestId}\"";
  }
  public bool IsValid()
  {
    return ProductId is not null && Filesize is not null && Filename is not null;
  }
}