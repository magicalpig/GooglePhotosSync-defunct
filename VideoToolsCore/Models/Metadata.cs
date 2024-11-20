using FFMpegCore;

namespace VideoToolsCore.Models;

public record Metadata
{
    private static readonly string UNKNOWN_CODEC = "unknown";
    
    public long? DurationMs { get; set; }
    public string Codec { get; set; } = UNKNOWN_CODEC;
    public DateTime? CreatedDate { get; set; }
    public string? Description { get; set; }

    public Metadata() {    }
    
    public Metadata(IMediaAnalysis ffmpegAnalysis) 
    {     
        DurationMs = (long)ffmpegAnalysis.Duration.TotalMilliseconds;

        Codec = ffmpegAnalysis.VideoStreams.FirstOrDefault()?.CodecName ?? UNKNOWN_CODEC;

        if (ffmpegAnalysis.Format?.Tags is Dictionary<string, string> tags)
        {
            if (tags.TryGetValue("creation_time", out string? dateString))
            {
                CreatedDate = DateTime.Parse(dateString);
            }
                
            List<string> descParts = ["title", "comment"];
            Description = string.Join(" | ", descParts
                .Select(k => tags.GetValueOrDefault(k))
                .Where(v => v is not null));
        }
    }

    public Metadata FillMissingFrom(Metadata? other)
    {
        if (other is null)
        {
            return this;
        }
        
        if ((DurationMs is null || DurationMs <= 0) && other.DurationMs > 0)
        {
            DurationMs = other.DurationMs;
        }
        if ((string.IsNullOrEmpty(Codec) || string.Equals(Codec, UNKNOWN_CODEC)) && !string.IsNullOrEmpty(other.Codec) && !string.Equals(other.Codec, UNKNOWN_CODEC))
        {
            Codec = other.Codec;
        }
        if (CreatedDate is null && other.CreatedDate is not null)
        {
            CreatedDate = other.CreatedDate;
        }
        if (string.IsNullOrEmpty(Description) && !string.IsNullOrEmpty(other.Description))
        {
            Description = other.Description;
        }
        return this;
    }

    public bool IsIncomplete()
    {
        return DurationMs <= 0 || string.IsNullOrEmpty(Codec)  || string.Equals(Codec, UNKNOWN_CODEC) || CreatedDate is null || string.IsNullOrEmpty(Description);
    }
}