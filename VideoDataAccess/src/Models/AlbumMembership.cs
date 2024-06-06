namespace VideoDataAccess.Models;

public class AlbumMembership(string videoFileGoogleId, string googleAlbumId)
{
  public string VideoFileGoogleId { get; set; } = videoFileGoogleId;
  public required VideoFileGoogle VideoFileGoogle { get; set; }

  public string GoogleAlbumId { get; set; } = googleAlbumId;
  public required GoogleAlbum GoogleAlbum { get; set; }

  public required string Owner { get; set; }
}