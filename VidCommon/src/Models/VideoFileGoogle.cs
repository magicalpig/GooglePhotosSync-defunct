using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CsvHelper.Configuration;

namespace VidCommon.Models;
[Table("GoogleVideos")]
public class VideoFileGoogle : VideoFileBase
{ 
  private static readonly string ShortUrlBase = "https://photos.google.com/photo/";

  [Key]
  public required string GoogleId { get; set; }

  // only gets set when setting RPCId
  [NotFromGoogleRestAPI]
  public string? Url { get; private set; }

  // this is the ID used in browser when calling the batchExecute RPC API
  private string? _rpcId;
  [NotFromGoogleRestAPI]
  public string? RpcId 
  { 
    get { return _rpcId; }
    set {
      _rpcId = value;
      Url = ShortUrlBase + value;
    }
  }


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

  // video instance pulled from REST API will not have certain properties that
  // can only come from JSON files (jsonToDb project). 
  public void SyncToRestAPIResult(VideoFileGoogle other)
  {
    // skip attempting to sync any properties that are not from the Google REST API
    // Do not overwritem properties that come from JSON files.
    var propsToUpdate = GetType().GetProperties()
      .Where(p => !Attribute.IsDefined(p, typeof(NotFromGoogleRestAPIAttribute)));
    
    foreach (var prop in propsToUpdate)
    {
      prop.SetValue(this, prop.GetValue(other));
    }
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