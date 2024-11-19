using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace VideoToolsCore.Models;

[Table("GoogleAlbums")]
public class GoogleAlbum : OwnableEntityBase
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public required string Id { get; set; }
  public string? Title { get; set; }
  // TODO: Maybe put the base url in config file
  [NotMapped]
  public string Url 
  { 
    get
    {
      return "https://photos.google.com/lr/album/" + Id;
    }
  }
  public int ItemCount { get; set; }
  public ICollection<AlbumMembership>? AlbumMemberships { get; set; }  
}
  