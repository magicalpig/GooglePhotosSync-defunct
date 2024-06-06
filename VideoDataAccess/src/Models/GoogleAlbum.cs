using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VideoDataAccess.Models;

[Table("GoogleAlbums")]
public class GoogleAlbum
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public required string Id { get; set; }
  public required string Owner { get; set; }
  public string? Title { get; set; }
  public string? Url { get; set; }
  public int ItemCount { get; set; }
  public ICollection<AlbumMembership>? AlbumMemberships { get; set; }  
}
  