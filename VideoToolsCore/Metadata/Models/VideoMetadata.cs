using System.Reflection;
using FFMpegCore;
using Microsoft.AspNetCore.Authorization;

namespace VideoToolsCore.Metadata.Models;

[AttributeUsage(AttributeTargets.Property)]
public class WritableMetadataAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public class DbColumnAttribute(string columnName) : Attribute
{
    public string ColumnName { get; } = columnName;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class MetadataFieldForWritingAttribute(string writerType, string fieldToWrite) : Attribute
{
    public string FieldToWrite { get; } = fieldToWrite;
    public string WriterType { get; } = writerType;
}

// defines the properties of a video file that can be scraped and in some cases written
// Summary of the properties:
//  Codec: Non-nullable string, defaults to "unknown"
//  CreatedDate: Nullable DateTime
//  Description: Nullable string
//  DurationMs: Nullable long value

public record VideoMetadata
{
    public static readonly string UNKNOWN_CODEC = "unknown";

    public long? DurationMs { get; set; }

    public string Codec { get; set; } = UNKNOWN_CODEC;

    [WritableMetadata]
    [MetadataFieldForWriting("ShellMetadataWriter", "System.Media.DateEncoded")]
    [DbColumn("CreatedDate")]
    public DateTime? CreatedDate { get; set; }

    [WritableMetadata]
    [MetadataFieldForWriting("ShellMetadataWriter", "System.Title")]
    [DbColumn("Description")]
    public string? Description { get; set; }

    public VideoMetadata() { }

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

    // this pertains to the question of whether it's worth trying to scrape additional metadata
    public bool IsIncomplete()
    {
        return !DurationMs.HasValue || DurationMs <= 0 || string.IsNullOrEmpty(Codec) || string.Equals(Codec, UNKNOWN_CODEC) || CreatedDate is null || string.IsNullOrEmpty(Description);
    }

    // This pertains to passing around a metadata object into metadata writers.
    // It signifies that there are still some values in the object that can be written to the file.
    public bool HasAnyWritableValues()
    {
        return GetType().GetProperties().Any(HasWritableValueForProperty);        
    }    
    public bool HasWritableValueForProperty(string propertyName)
    {
        var property = GetType().GetProperty(propertyName) ?? throw new ArgumentException($"No property found with name {propertyName}");
        return HasWritableValueForProperty(property);        
    }
    public bool HasWritableValueForProperty(PropertyInfo property)
    {
        var value = property.GetValue(this);
        return IsPropertyWritable(property) && value != null && !(property.Name == "Codec" && value.ToString() == UNKNOWN_CODEC);
    }

    // Regardless of whethe the property has a value, is the property writable?
    public static bool IsPropertyWritable(string propertyName)
    {
        var property = typeof(VideoMetadata).GetProperty(propertyName);
        return property != null && IsPropertyWritable(property);
    }
    public static bool IsPropertyWritable(PropertyInfo property)
    {
        return property.GetCustomAttributes(typeof(WritableMetadataAttribute), false).Length > 0;
    }

    public static string? GetDbColumnForMetadataProperty(string propertyName)
    {
        var property = typeof(VideoMetadata).GetProperty(propertyName);
        var dbColumnAttribute = property?.GetCustomAttributes(typeof(DbColumnAttribute), false).FirstOrDefault() as DbColumnAttribute;
        return dbColumnAttribute?.ColumnName;
    }

    // for a given metadata writer type and a given VideoMetadata property, get the name of the field to write to
    public static string? GetMetadataWritableField(string writerType, PropertyInfo property)
    {
        var attribute = property.GetCustomAttributes(typeof(MetadataFieldForWritingAttribute), false)
                                .Cast<MetadataFieldForWritingAttribute>()
                                .FirstOrDefault(a => a.WriterType == writerType);
        return attribute?.FieldToWrite;
  
    }
}