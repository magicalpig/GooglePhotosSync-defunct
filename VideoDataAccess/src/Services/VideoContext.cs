using Microsoft.EntityFrameworkCore;
using VideoDataAccess.Models;

namespace VideoDataAccess.Services;


// no one is allowed to create a VideoContext. The constructor is internal. Must use VideoService. 
// Usually this will be UserScopedVideoService which ensures db operations are scoped to the current user.

public class VideoContext : DbContext
{
  public DbSet<VideoFileGoogle> GoogleVideos { get; set; }
  public DbSet<VideoFileWindows> WindowsVideos { get; set; }

  private readonly string _connectionString;
  internal VideoContext (string connectionString)
  {
    _connectionString = connectionString;
  }
  protected override void OnConfiguring(DbContextOptionsBuilder options)
  {
    options.UseSqlite(_connectionString);
  }
}