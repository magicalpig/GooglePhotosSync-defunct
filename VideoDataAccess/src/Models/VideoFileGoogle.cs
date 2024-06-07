using System.ComponentModel.DataAnnotations.Schema;
using CsvHelper.Configuration;

namespace VideoDataAccess.Models;
[Table("GoogleVideos")]
public class VideoFileGoogle : VideoFileBase
{ 
  private static readonly string ShortUrlBase = "https://photos.google.com/photo/";
  [FromGoogleRestApi]
  public required string GoogleId { get; set; }

  // only gets set when setting RPCId
  public string? Url { get; private set; }

  // this is the ID used in browser when calling the batchExecute RPC API
  private string? _rpcId;
  public string? RpcId 
  { 
    get { return _rpcId; }
    set {
      _rpcId = value;
      Url = ShortUrlBase + value;
    }
  }
  
  public ICollection<AlbumMembership>? AlbumMemberships { get; set; }

  public override bool Equals(object? obj)
  {
    if (obj == null || GetType() != obj.GetType())
    {
      return false;
    }

    VideoFileGoogle other = (VideoFileGoogle)obj;

    return base.Equals(obj) &&
         GoogleId == other.GoogleId &&
         Url == other.Url;
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(base.GetHashCode(), GoogleId, Url);
  }

  // for updating a google video record with data pulled from the REST API
  public void SyncToRestAPIResult(VideoFileGoogle other)
  {
    // skip attempting to sync any properties that are not from the Google REST API    
    var propsToUpdate = GetType().GetProperties()
      .Where(p => Attribute.IsDefined(p, typeof(FromGoogleRestApiAttribute)));
    
    foreach (var prop in propsToUpdate)
    {
      prop.SetValue(this, prop.GetValue(other));
    }
  }

  public static bool IsRestApiMetadataDifferent(VideoFileGoogle a, VideoFileGoogle b)
  {
    var propsToCompare = typeof(VideoFileGoogle).GetProperties()
      .Where(p => Attribute.IsDefined(p, typeof(FromGoogleRestApiAttribute)));

    foreach (var prop in propsToCompare)
    {
      if (!Equals(prop.GetValue(a), prop.GetValue(b)))
      {
        Console.WriteLine($"Property {prop.Name} differs between {a.GoogleId} and {b.GoogleId}");
        return true;
      }
    }
    return false;
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
    Map(m => m.Url).Index(7).Name("Google URL");
  }
}