using VideoDataAccess.Models;

namespace VideoDataAccess.Services;
public interface IVideoService
{
  // using "= null" on the argument allows us to call the method without passing in a value
  IQueryable<VideoFileGoogle> GetGoogleVideos(string? userId = null);
  IQueryable<VideoFileWindows> GetWindowsVideos(string? userId = null);
  void RemoveRange(IEnumerable<VideoFileBase> videos, string? userId = null);
  void RemoveAllVideos(string? userId = null);
  void Add<T>(T video, string? userId = null) where T : VideoFileBase;
  void Update<T>(T video, string? userId = null) where T : VideoFileBase;
  void SaveChanges();
}