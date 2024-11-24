using System.ComponentModel.DataAnnotations.Schema;
namespace VideoToolsCore.Models;

public class AlbumMembership(string videoId, string albumId) : OwnableEntityBase
{
  public string VideoId { get; set; } = videoId;
  public  VideoFileGoogle VideoFileGoogle { get; set; } = default!;

  public string AlbumId { get; set; } = albumId;
  public GoogleAlbum GoogleAlbum { get; set; } = default!;

  [NotMapped]
  public bool FoundInLatestScrape { get; set; }
}