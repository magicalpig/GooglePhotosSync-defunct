# VideoScraper Solution
Gather info about videos uploaded to Google Photos and present in local filesystem.  
Details are stored in a SQLite database named video_files.db.
The purpose of this solution is to identify 
- videos present on Google Photos but not local file system.
- videos present on local file system but no yet uploaded to GP.
- differences in GP and local copy of video (maybe I re-encoded a video from AVC to HEVC)
- find videos on GP or locally that have same name
- review metadata that might need correcting.

## Usage Notes
Much of the metadata about Google Videos can be scraped from its REST API. The `GoogleScraper` project does that. It identifies videos by what we call the REST ID. These are about 98 (or maybe exactly?) characters long.  
This technique does not give us the size or duration of the videos. To get this, we need to use our web browsers to interact with the Google Photos website. We run Tampermonkey to make the gptk.js script available within the browser. I have modified this script so it has a "Scrape Videos" action. More details on this later when I discuss the 2 different video ID schemes Google Photos uses.

## Solution Layout
There are 4 projects here:  
| | |
| --- | --- |
| **VideoToolsCore** | Base functionality used by multiple projects including database access, models, helpers for building the app (loading configuration files, logging), and helpers for scraping local filesystem metadata using FFMpeg |
| **LocalScaper** | Scans local filesystem for videos; collects metadata using ffprobe and shell32; stores in database; deletes records for videos not found in scanned directory |
| **GoogleScraper** | Connects to Google Photos API to get information on videos and albums; stores in database; offers to delete record from db if no longer present in GP |
| **WebHelper** | Runs a webserver to listen for JSON payloads sent from browser. Used for obtaining Google Photos metadata not available through Google's REST API |

## Database
### Table: GoogleVideos
Primary key is ID used by REST API (the one GoogleScraper connects to) AND owner (email of Google Photos account). Some of the data comes from the REST API (running GoogleScraper) while some of it comes from JSON files obtained through the browser using the batchExecute RPC API.

### Table: WindowsVideos
Primary key is full path to video AND owner (user running the LocalScraper app, Environment.UserName).

### Table: GoogleAlbums
Names and IDs of albums on Google Photos.

### AlbumMemberships
Connects GoogleVideos to GoogleAlbums.

## Prerequisites
- relies on ffprobe.exe which is part of the ffmpeg distribution. So install ffmpeg and then set `FFMpegPath` in the appsettings`{environment}`.json file.
- app needs to be registered on Google cloud console as a "UWP" app. After obtaining client id, store this in the appsettings`{environment}`.json file as `GoogleClientId`.  

  The application was registered on google's cloud console and issued a client id. A client secret was also issued, but we don't want to use the client secret. Let's pretend this is an app that is distributed to the end user and meant to run on their personal computer. We can't give out the client secret. What we're doing instead is relying on the PKCE extension to OAuth2.0, which the Google authorization libraries enable by default. However, when I first registered this app on the Google Cloud console as type "desktop", I was having to send the client secret along to Google when the user authenticates and authorizes the app. To get around this, I registered the app again but as a "UWP" app and now when I use the client id associated with the "UWP" version, I'm not required to send the client secret.
## Configuration
- **VideoDataAccess** Uses DbSettings.json to define database file as `DbFileName`
- **Local and Google Scrapers** These use `appsettings.json` and `appsettings.Development.json` to define settings. Here is an example of what the appsettings.json file should look like:
```json{
  "Settings": {
    "LocalUserId": "Rob",
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
  | LocalUserId | has no effect other than storing access tokens to Google's API. This user id doesn't necessarily need to match a Google account or a local user account|
  | FileLimitLocal | how many local video files to process| 
  | PageLimitGoogle | how many pages of GP videos to query |
  | FreshDatabase | whether to erase all records from the WindowsVideos and GoogleVideos tables in database when program starts | 
  | GoogleClientId | client id issued after registering app on Google Cloud Console|
  | SourceDir |  local folder in which to look (subdirectories will be included). This can be defined at the top level or at the machine level to override top level|
  | FFMpegPath | Location of ffmpeg (more importantly ffprobe) executable|
  
Notice that under `MachineSettings`, the keys at the next level down are machine names. This allows you to configure the program differently depending on where it's being run from. This machine name doesn't need to match actual case and can exceed the 15-char limit for NETBIOS names (although only first 15 chars will be examined when trying to locate correct key to use at runtime).

Remember that if you are in development and use appsettings.Development.json, appsettings.json will still be loaded first and then any settings in the development file will overwrite those in the main file.
  

## About the Google Photos API and the need for web browser
The GP REST API doesn't expose video filesize or duration. To get these metadata, you manually browse to the GP website and you use something like Tampermonkey to run the user script included in this solution as gptk.js. This user script is a modified version of something I found online.

This GPTK script makes use of the poorly documented "batchExecute" RPC API of Google Photos. There are certain operations you could ask it to do that will cause it to query the RPC API for information about all videos. I made modifications to the script so it will collect metadata about all videos and post the result to the web server app included in this solution. This is triggered by the "Scrape Videos" button I added.

Where things get tricky is in the fact that Google's REST API uses a different ID format than what is used by their RPC API. The browser will send videos to our web server app identifying each video by its ID for the RPC API. When the web server app looks for a database record to update, those records will only have an ID used by the REST API. The web server app will respond to the web browser telling it all the REST API IDs that aren't yet paired up with an RPC API ID. The web browser will then make HEAD requests for videos where the REST ID is in the URL. Google Photos will respond with a redirect to a URL that includes the RPC ID instead. Now we have our mapping between REST ID and RPC ID. The videos are once again sent to our web server app this time including both the REST API ID and the RPC API ID. The web server app will be able to locate a record in the database using the REST API ID and set the newly scraped metadata as well as the RPC API ID.

# Weirdness
The GooglePhotosService from the CasCap.Apis.GooglePhotos library has to share the exact same HttpClient instance as is used by my GoogleApiCurrentUserService service. This is because GoogleApiCurrentUserService needs to perform the authorization to Google and when the authorization succeeds it needs to store an Authorization header as part of the HttpClient's default headers.  
The reason GoogleApiCurrentUserService needs to perform the authorization is that the GooglePhotosService insists on there being a client secret. As stated before, we are trying to rely only on PKCE and we are not sending any client secret nor storing any client secret in the app.

# TODO
- document how to run from command line including passing arguments, setting Development/Production environment variable.


# Technical Notes
The database is accessed through a DbContext subclass, which is standard EF Core stuff.  I am using a subclass called `VideoContext` that can be given an `ICurrentUserService` to apply the current user's ID to each db transaction. DB read operations automatically apply this filter (see the `OnConfiguring` method where is makes use `HasQueryFilter`), but updates and adds do not automatically get scoped down to the current user. To account for this, I had to add new Update and Add methods in the `VideoContext` class that would attach the user id to each video before tracking its changes.

The `VideoContext` has an internal constructor and is meant to be created by a factory called `VideoContextFactory`. One of the factory methods is `CreateUserScopedVideoContext`. It takes a type param for the type of `ICurrentUserService` to use to find the current user's ID. The other factory method is just plain `CreateVideoContext`. This one is used when the current user's ID is not needed. I haven't thought of such a scenario yet so I throw an exception when that method is called.

There's also something called the `DesignTimeDbContextFactory` which I had to create in order to give the EF Core tools a way to create a `VideoContext` (whose constructor is internal) when I'm running command line tools like `dotnet ef migrations add AddedNewColumn`.

Access Tokens for the Google API will be stored in C:\Users\\&lt;username>\AppData\Roaming\Google.Apis.Auth. The token will be named according to the `LocalUserId` setting in appsettings.json.

