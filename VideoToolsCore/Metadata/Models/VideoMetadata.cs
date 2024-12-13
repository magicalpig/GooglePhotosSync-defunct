using System.Reflection;
using FFMpegCore;

namespace VideoToolsCore.Metadata.Models;

[AttributeUsage(AttributeTargets.Property)]
public class WritableMetadataAttribute : Attribute { }


[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class MetadataFieldForWritingAttribute(string writerType, string fieldToWrite, string? fileExtension = null) : Attribute
{
    public string WriterType { get; } = writerType;
    public string FieldToWrite { get; } = fieldToWrite;
    public string? FileExtension { get;} = fileExtension;
}

// defines the properties of a video file that can be scraped and in some cases written
// Summary of the properties:
//  Codec: Non-nullable string, defaults to "unknown". Not used for Google videos
//  CreatedDate: Nullable DateTime
//  Description: Nullable string
//  DurationMs: Nullable long value

public record VideoMetadata
{
    public long? DurationMs { get; set; }

    public static readonly string UNKNOWN_CODEC = "unknown";
    public string Codec { get; set; } = UNKNOWN_CODEC;
    
    [WritableMetadata]
    [MetadataFieldForWriting("ShellMetadataWriter", "System.Media.DateEncoded")]
    [MetadataFieldForWriting("FFMpegMetadataWriter", "creation_time")]
    [MetadataFieldForWriting("FFMpegMetadataWriter", "ICRD", "avi")]
    public DateTime? CreatedDate
    {
        get => _createdDate;
        // setter has extra functionality to remove milliseconds from the date
        set
        {
            if (value.HasValue)
            {
                DateTime d = value.Value;
                // clear milliseconds off date stamp
                d = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second, 0, d.Kind);
                _createdDate = d;
            }
            else
            {
                _createdDate = null;
            }
        }
    }
    private DateTime? _createdDate;


    [WritableMetadata]
    [MetadataFieldForWriting("ShellMetadataWriter", "System.Title")]
    [MetadataFieldForWriting("FFMpegMetadataWriter", "title")]
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
            // most formats have a "creation_time" tag; avi files have a "date" tag
            if (tags.TryGetValue("creation_time", out string? dateString) || tags.TryGetValue("date", out dateString))
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

    // this pertains to the question of whether it's worth trying to scrape additional metadata.
    // Notice it doesn't check for codec, because that's not a required field for Google videos.
    public bool IsIncomplete()
    {
        return !DurationMs.HasValue || DurationMs <= 0 || CreatedDate is null || string.IsNullOrEmpty(Description);
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

    // Regardless of whether the property has a value, is the property writable?
    public static bool IsPropertyWritable(string propertyName)
    {
        var property = typeof(VideoMetadata).GetProperty(propertyName);
        return property != null && IsPropertyWritable(property);
    }
    public static bool IsPropertyWritable(PropertyInfo property)
    {
        return property.GetCustomAttributes(typeof(WritableMetadataAttribute), false).Length > 0;
    }
    
    // for a given metadata writer type and a given VideoMetadata property, get the name of the field to write to
    // If a file extension is provided, prefer any attribute that specifies that extension
    // otherwise return the default attribute which no file extension specified
    public static string? GetMetadataWritableField(string writerType, PropertyInfo property, string? fileExtension = null)
    {
        var attributesForWriter = property
                                    .GetCustomAttributes(typeof(MetadataFieldForWritingAttribute), false)
                                    .Cast<MetadataFieldForWritingAttribute>();
        var attribute = 
            attributesForWriter.FirstOrDefault(a => a.WriterType == writerType && a.FileExtension?.ToLower() == fileExtension?.ToLower())
            ?? attributesForWriter.FirstOrDefault(a => a.WriterType == writerType && a.FileExtension == null);
                                
        return attribute?.FieldToWrite;
    }
    
    public bool HasAnyValues()
    {
        return GetType().GetProperties().Any(p => p.GetValue(this) != null && (p.Name != "Codec" || (string?)p.GetValue(this) != "UNKNOWN_CODEC"));
    }
}