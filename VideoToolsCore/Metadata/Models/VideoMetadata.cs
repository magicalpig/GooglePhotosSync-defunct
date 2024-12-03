using System.Reflection;
using FFMpegCore;

namespace VideoToolsCore.Metadata.Models;

[AttributeUsage(AttributeTargets.Property)]
public class OverwritableMetadataAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public class DbColumnAttribute(string columnName) : Attribute
{
    public string ColumnName { get; } = columnName;
}

[AttributeUsage(AttributeTargets.Property)]
public class ShellPropertyAttribute(string shellPropertyName) : Attribute
{
    public string ShellPropertyName { get; } = shellPropertyName;
}

// defines the properties of a video file that can be scraped and in some cases written
public record VideoMetadata
{
    public static readonly string UNKNOWN_CODEC = "unknown";
    
    public long? DurationMs { get; set; }

    public string Codec { get; set; } = UNKNOWN_CODEC;

    [OverwritableMetadata]
    [ShellProperty("System.Media.DateEncoded")]
    [DbColumn("CreatedDate")]
    public DateTime? CreatedDate { get; set; }

    [OverwritableMetadata]
    [ShellProperty("System.Title")]
    [DbColumn("Description")]
    public string? Description { get; set; }

    public VideoMetadata() {    }
    
    public VideoMetadata(IMediaAnalysis ffmpegAnalysis) 
    {     
        DurationMs = (long)ffmpegAnalysis.Duration.TotalMilliseconds;

        Codec = ffmpegAnalysis.VideoStreams.FirstOrDefault()?.CodecLongName
                ?? ffmpegAnalysis.VideoStreams.FirstOrDefault()?.CodecTagString
                ?? UNKNOWN_CODEC;

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

    public VideoMetadata FillMissingFrom(VideoMetadata? other)
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

    public static bool IsPropertyOverwritable(string propertyName)
    {        
        var property = typeof(VideoMetadata).GetProperty(propertyName);
        return property != null && IsPropertyOverwritable(property);
    }
    public static bool IsPropertyOverwritable(PropertyInfo property)
    {
        return property.GetCustomAttributes(typeof(OverwritableMetadataAttribute), false).Length > 0;
    }

    public static string? GetDbColumnForMetadataProperty(string propertyName)
    {
        var property = typeof(VideoMetadata).GetProperty(propertyName);
        var dbColumnAttribute = property?.GetCustomAttributes(typeof(DbColumnAttribute), false).FirstOrDefault() as DbColumnAttribute;
        return dbColumnAttribute?.ColumnName;
    }

    public static string? GetShellPropertyForMetadataProperty(PropertyInfo property)
    {        
        var shellPropertyAttribute = property?.GetCustomAttributes(typeof(ShellPropertyAttribute), false).FirstOrDefault() as ShellPropertyAttribute;
        return shellPropertyAttribute?.ShellPropertyName;
    }
}