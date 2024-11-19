using System.ComponentModel.DataAnnotations.Schema;
namespace VideoToolsCore.Models;

public class AlbumMembership(string videoFileGoogleId, string googleAlbumId) : OwnableEntityBase
{
  public string VideoFileGoogleId { get; set; } = videoFileGoogleId;
  public  VideoFileGoogle VideoFileGoogle { get; set; } = default!;

  public string GoogleAlbumId { get; set; } = googleAlbumId;
  public GoogleAlbum GoogleAlbum { get; set; } = default!;

  [NotMapped]
  public bool FoundInLatestScrape { get; set; }
}