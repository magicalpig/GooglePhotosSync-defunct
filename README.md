# VideoScraper
Gather info about videos uploaded to Google Photos and present in local filesystem.  
Details are stored in a SQLite database named video_files.db.
The purpose of this app is to identify 
- videos present on Google Photos but not local file system.
- videos present on local file system but no yet uploaded to GP.
- differences in GP and local copy of video (maybe I re-encoded a video from AVC to HEVC)
- find videos on GP or locally that have same name
- review metadata that might need correcting.

## Solution Layout
There are 3 projects here:  
| | |
| --- | --- |
| **VidCommon** | Establishes the data models and the database connection (DbContext in .NET parlance). Included by the other projects |
| **jsonToDb** | Takes JSON obtained from a web browser session with Google Photos as a means to add metadata not otherwise obtainable through the GP API. |
| **vidprobe** | Scans local filesystem for videos and connects to GP API to get its videos. Stores newly discovered videos into database. Update existing records. Offers to delete record from db if no longer present in GP |  

## Database
### GoogleVideos
Primary key is ID used by REST API (the one vidprobe connects to).  
Has columns for DurationMs (raw), Duration (human-readable), and Size but I think that's a mistake.
### WindowsVideos
Primary key is full path to video.
### GoogleVideosMetadataFromChrome
Primary key is ID used by the batchExecute RPC Api (the one you'd use from a web browser on the GP website).
This has columns for DurationMs, and Size. I believe the intention is to keep this stuff only in this table and join GoogleVideos table against this table when you want that stuff.

## Prerequisites
- relies on ffprobe.exe which is part of the ffmpeg distribution. So install ffmpeg and then set `FFMpegPath` in the application`{environment}`.json file.
- app needs to be registered on Google cloud console as a "UWP" app. After obtaining client id, store this as a dotnet user secret using `dotnet user-secrets set "GoogleAPI:client_id" "your client id"`. This should be done from inside the `vidprobe` sub directory.   
The application was registered on google's cloud console and issued a client id. A client secret was also issued, but we don't want to use the client secret. Let's pretend this is an app that is distributed to the end user and meant to run on their personal computer. We can't give out the client secret. What we're doing instead is relying on the PKCE extension to OAuth2.0, which the Google authorization libraries enable by default. However, when I first registered this app on the Google Cloud console as type "desktop", I was having to send the client secret along to Google when the user authenticates and authorizes the app. To get around this, I registered the app again but as a "UWP" app and now when I use the client id associated with the "UWP" version, I'm not required to send the client secret.
## Configuration
- **VidCommon** Uses CommonSetting.json to define database file as `DbFileName`
- **jsonToDb** Hardcoded to look at `GoogleVideosMetadata.json` for the JSON containing GP video filesizes and `GoogleVideosDurations.json` for the GP video durations and `RestToRpc.json` for mapping between 2 different ID schemes GP uses (more later).
- **vidprobe** Example:
```json{
  "Settings": {  
    "FileLimitLocal": 1,  
    "PageLimitGoogle": 1,  
    "FreshDatabase": false  
  },  
  "Logging": {  
    "LogLevel": {  
      "Default": "Debug"  
    }  
  },  
  "MachineSettings": {  
    "DellStudioXPS8000" : {
      "SourceDir": "D:/homevideos",
      "FFMpegPath": "c:/ffmpeg/bin/"
    },
    "WINDOWS-DU9EAQV" : {
      "SourceDir": "c:/temp",
      "FFMpegPath": "c:/ffmpeg/binx/"
    }
  }
}
```
  | | |
  | --- | --- |
  | FileLimitLocal | how many local video files to process| 
  | PageLimitGoogle | how many pages of GP videos to query |
  | FreshDatabase | whether to erase all records from the WindowsVideos and GoogleVideos tables in database when program starts. Note that the GoogleVideosMetadataFromChrome table (populated by jsonToDb) is unaffected | 
  | SourceDir |  local folder in which to look (subdirectories will be included)|
  | FFMpegPath | Location of ffmpeg (more importantly ffprobe) executable|
  
Notice that under `MachineSettings`, the keys at the next level down are machine names. This allows you to configure the program differently depending on where it's being run from. This machine name doesn't need to match actual case and can exceed the 15-char limit for NETBIOS names (although only first 15 chars will be examined when trying to locate correct key to use at runtime).
  

## About the Google Photos API and the need for web browser
The GP API doesn't expose video filesize or duration. To get these metadata, you manually browse to the GP website and you use something like Tampermonkey to run the custom script called "GPTK" https://github.com/xob0t/Google-Photos-Toolkit/  

This GPTK script makes use of the poorly documented "batchExecute" RPC API of Google Photos. There are certain operations you could ask it to do that will cause it to query the RPC API for information about all videos. I made modifications to the script so as it queried the API, metadata about videos would be set aside for me to later save as JSON. The modified source code is in the jsonToDb project folder as `gptk.js`.   

One way to cause GPTK to get all videos is to
1. make a dummy album
2. add filter for Type=Video
3. add filter for Size < 2 KB
4. make sure "Library" is selected as your source along the top
5. perform the operation to add matching videos to an album, specifying your dummy album.

After this completes, you'll have a `durations` object. I must have lost other changes I had made to the script because I'm not seeing anything that would produce the other 2 datastructures we need: one for filesizes and one for mapping the RPC ID to the REST ID.  
I should explain that last point:  
The batchExecute RPC system refers to videos by an ID that is not the same as how the REST API refers to them. The RPC ones are shorter and are the ones that can be used in a URL in your browser such as https://photos.google.com/photo/{RPC ID}. We're using the REST ID as the key in the database, therefore, we need a mapping between the IDs.

### But again, I don't see anything in the GPTK script that will capture the video filesizes or the RPC-to-REST ID mapping.


