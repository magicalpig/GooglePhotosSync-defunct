# VideoScraper
Gather info about videos uploaded to Google Photos and present in local filesystem.  
Details are stored in a SQLite database named video_files.db.
The purpose of this app is to identify 
- videos present on Google Photos but not local file system.
- videos present on local file system but no yet uploaded to GP.
- differences in GP and local copy of video (maybe I re-encoded a video from AVC to HEVC)
- find videos on GP or locally that have same name
- review metadata that might need correcting.

## Usage Notes
Much of the metadata about Google Videos can be scraped from its REST API. The `vidprobe` project does that. It identifies videos by what we call the REST ID. These are about 98 (or maybe exactly?) characters long.  
This technique does not give us the size or duration of the videos. To get this, we need to use our web browsers to interact with the Google Photos website. We run Tampermonkey to make the gptk.js script available within the browser. I have modified this script so it has a "Scrape Videos" action.  
Scrape Videos will invoke Google's "batchExecute" RPC API. Following these invocations, the browser will have information about the videos' duration and size.   
The custom userscript running in Tampermonkey will then post these videos to our WebHelper web server. My plan is to have the WebHelper insert these videos into these database or update records that are already there. The problem I'm facing now is that the RPC calls assign a different type of ID to each video. These are only 43 characters long. I need a way to map the IDs from the RPC calls to the IDs from the REST calls.  
What I might do is have the WebHelper respond with some javascript that can be executed. This javascript will make HEAD calls to GooglePhotos for specific videos using the REST ID. I've found that Google will redirect these requests to one that uses the shorter RPC ID.

## Solution Layout
There are 4 projects here:  
| | |
| --- | --- |
| **VideoDataAccess** | Establishes the data models and the database connection (DbContext in .NET parlance). Included by the other projects |
| **jsonToDb** | Takes JSON obtained from a web browser session with Google Photos as a means to add metadata not otherwise obtainable through the GP API. |
| **vidprobe** | Scans local filesystem for videos and connects to GP API to get its videos. Stores newly discovered videos into database. Update existing records. Offers to delete record  from db if no longer present in GP |
| **WebHelper** | Runs a webserver to listen for JSON payloads sent from browser. This will eventually replace jsonToDb |

## Database
### Table: GoogleVideos
Primary key is ID used by REST API (the one vidprobe connects to). Some of the data comes from the REST API (running vidprobe) while some of it comes from JSON files obtained through the browser using the batchExecute RPC API (you get this data into the db by running jsonToDb).

### Table: WindowsVideos
Primary key is full path to video.

## Prerequisites
- relies on ffprobe.exe which is part of the ffmpeg distribution. So install ffmpeg and then set `FFMpegPath` in the application`{environment}`.json file.
- app needs to be registered on Google cloud console as a "UWP" app. After obtaining client id, store this in the application`{environment}`.json file as `GoogleClientId`.  

  The application was registered on google's cloud console and issued a client id. A client secret was also issued, but we don't want to use the client secret. Let's pretend this is an app that is distributed to the end user and meant to run on their personal computer. We can't give out the client secret. What we're doing instead is relying on the PKCE extension to OAuth2.0, which the Google authorization libraries enable by default. However, when I first registered this app on the Google Cloud console as type "desktop", I was having to send the client secret along to Google when the user authenticates and authorizes the app. To get around this, I registered the app again but as a "UWP" app and now when I use the client id associated with the "UWP" version, I'm not required to send the client secret.
## Configuration
- **VideoDataAccess** Uses CommonSetting.json to define database file as `DbFileName`
- **jsonToDb** Hardcoded to look at `GoogleVideosMetadata.json` for the JSON containing GP video filesizes and `GoogleVideosDurations.json` for the GP video durations and `RestToRpc.json` for mapping between 2 different ID schemes GP uses (more later).
- **vidprobe** Example:
```json{
  "Settings": {  
    "FileLimitLocal": 1,  
    "PageLimitGoogle": 1,  
    "FreshDatabase": false,
    "GoogleClientId": "your-client-id-here"
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
  | FreshDatabase | whether to erase all records from the WindowsVideos and GoogleVideos tables in database when program starts | 
  | GoogleClientId | client id issued after registering app on Google Cloud Console|
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

# Weirdness
The GooglePhotosService from the CasCap.Apis.GooglePhotos library has to share the exact same HttpClient instance as is used by my GoogleConnector service. This is because GoogleConnector needs to perform the authorization to Google and when the authorization succeeds it needs to store an Authorization header as part of the HttpClient's default headers.  
The reason GoogleConnector needs to perform the authorization is that the GooglePhotosService insists on there being a client secret. As stated before, we are trying to rely only on PKCE and we are not sending any client secret nor storing any client secret in the app.

# TODO
- Figure out if metadata from chrome is supposed to be its own table and if so, then why does GoogleVideos table have columns that duplicate what's in metadata from chrome table. What are implications on model classes and EF serialization?
- document how to run from command line including passing arguments, setting Development/Production environment variable.
- nothing is populating GoogleVideos "URL" column.

# Technical Notes
Regarding the area of code concerned with data access, there is a potentially confusing structure of objects, interfaces, and factories.  

Data access goes through a DbContext, which is standard EF Core stuff.  But I have a subclass called `VideoContext` that says to use SQLite and has named datasets: `GoogleVideos` and `WindowsVideos`.  

The `VideoContext` is created by a factory called `VideoContextFactory`. The factory provides the connection string and is also useful because `VideoContext` has an `internal` constructor.  

The factory's method to create a `VideoContext` is also `internal`. Application code is not supposed to own and use a factory directly. Instead, the factory is encapsulated by a `VideoService` object.  

A `VideoService` proxies certain method calls to the underlying `VideoContext` acting like a facade. This is the only way the application code is meant to interact with the `VideoContext` and thus the database.  

`UserScopedVideoService` is a subclass of `VideoService` that ensures all database operations are scoped down to the current user.  This type of video service has an `ICurrentUserService` that tells it how to get the current user id, whether this is the Google account on whose behalf we are hitting the Google REST API, or scraping the local filesystem, etc.  

I ended up feeling like I had to create a factory for `UserScopedVideoService` so that the app code had a way to tell it which specific `ICurrentUserService` implementation to use.  If memory serves me correctly, things got complicated in the vidprobe project because I wanted 2 flavors of `UserScopedVideoService` during the lifetime of a single run of the program: one for the local file system and one for hitting the Google REST API.  

There's also something called the `DesignTimeDbContextFactory` which I had to create in order to give the EF Core tools a way to create a `VideoContext` (whose constructor is internal) when I'm running command line tools like `dotnet ef migrations add AddedNewColumn`.

