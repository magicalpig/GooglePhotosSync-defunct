using System.ComponentModel.DataAnnotations.Schema;
using VideoToolsCore.Attributes;

namespace VideoToolsCore.DataAccess.Models;


[Table("GoogleVideos")]
public class VideoFileGoogle : VideoFileBase
{
    private static readonly string ShortUrlBase = "https://photos.google.com/photo/";
    [FromGoogleRestApi]
    public required string RestId { get; set; }

    // only gets set when setting ProductId
    public string? Url { get; private set; }

    // this is the ID used in browser when calling the batchExecute RPC API
    private string? _productId;
    public string? ProductId
    {
        get { return _productId; }
        set
        {
            _productId = value;
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
             RestId == other.RestId &&
             Url == other.Url;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), RestId, Url);
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
                Console.WriteLine($"Property {prop.Name} differs between {a.RestId} and {b.RestId}");
                return true;
            }
        }
        return false;
    }
}

