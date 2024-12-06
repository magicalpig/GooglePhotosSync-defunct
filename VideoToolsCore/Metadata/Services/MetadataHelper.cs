using Microsoft.Extensions.Logging;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Readers;
using VideoToolsCore.Metadata.Writers;
using VideoToolsCore.Metadata.Models;

namespace VideoToolsCore.Metadata.Services;

public class MetadataHelper(ILogger<MetadataHelper> logger, IEnumerable<IMetadataReader> metadataReaders, IEnumerable<IMetadataWriter> metadataWriters)
{
    private readonly ILogger<MetadataHelper> _logger = logger;

    private readonly List<IMetadataReader> _availableReaders =
        metadataReaders
            .Where(r => r is not null)
            .OrderBy(r => r.Priority)
            .ToList();
    private readonly List<IMetadataWriter> _availableWriters =
        metadataWriters
            .Where(w => w is not null)
            .OrderBy(w => w.Priority)
            .ToList();


    // Combines metadata obtained through each available metadata reader
    public async Task<VideoMetadata?> GetMetadataAsync(VideoFileWindows videoFile)
    {
        var readerIndex = 0;
        VideoMetadata? metadataCollected = null;
        while (readerIndex < _availableReaders.Count && !MetadataHelper.IsMetadataCompleteForWindows(metadataCollected))
        {
            var reader = _availableReaders[readerIndex];
            try
            {
                _logger.LogDebug("Attempting metadata read using {readerType} for {filePath}", reader.GetType().Name, videoFile.Path);
                var thisReaderMetadata = await reader.GetMetadataAsync(videoFile);
                metadataCollected = metadataCollected?.FillMissingFrom(thisReaderMetadata) ?? thisReaderMetadata;
            }
            catch (Exception e)
            {
                _logger.LogError("Error reading metadata for file {filePath}: {errMessage}", videoFile.Path, e.Message);
            }
            finally
            {
                readerIndex++;
            }
        }

        return metadataCollected;
    }

    // Tries to write all metadata provided in the VideoMetadata argument to the video file using available writers
    public async Task<bool> WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata metadata)
    {
        var somethingChangedSuccessfully = false;
        // while metadata has non-null properties and there are still some writers to try
        var writerIndex = 0;
        while (writerIndex < _availableWriters.Count && metadata.HasAnyWritableValues())
        {
            var writer = _availableWriters[writerIndex];
            if (writer.CanWriteAny(metadata))
            {
                _logger.LogDebug("Attempting metadata write using <{writerType}> for {filePath}", writer.GetType().Name, videoFile.Path);
                var writtenProperties = await writer.WriteMetadataAsync(videoFile, metadata);
                
                if (writtenProperties.Count == 0)
                {
                    _logger.LogWarning("No metadata properties written by <{writerType}> for {filePath}", writer.GetType().Name, videoFile.Path);
                }
                else
                {
                    somethingChangedSuccessfully = true;
                    // null out the written property
                    foreach (var prop in writtenProperties)
                    {
                        _logger.LogInformation("SUCCESS: Wrote {prop} to {filePath}", prop, videoFile.Path);
                        // null out the property just written
                        metadata.GetType().GetProperty(prop)?.SetValue(metadata, null);
                    }                    
                }
            }
            else
            {
                _logger.LogDebug("Writer type <{writerType}> cannot write any of the metadata properties provided", writer.GetType().Name);
            }

            writerIndex++;
        }

        return somethingChangedSuccessfully;
    }

    public static bool IsMetadataCompleteForWindows(VideoMetadata? metadata)
    {
        return metadata is not null && !metadata.IsIncomplete() 
            && !string.IsNullOrEmpty(metadata.Codec) && !string.Equals(metadata.Codec, VideoMetadata.UNKNOWN_CODEC);
    }
}