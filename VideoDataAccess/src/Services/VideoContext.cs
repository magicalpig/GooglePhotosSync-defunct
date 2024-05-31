using Microsoft.EntityFrameworkCore;
using VideoDataAccess.Models;

namespace VideoDataAccess.Services;


// A VideoContext should only be created by a VideoContextFactory: internal constructor
public class VideoContext : DbContext
{
  public DbSet<VideoFileGoogle> GoogleVideos { get; set; }
  public DbSet<VideoFileWindows> WindowsVideos { get; set; }

  private readonly string _connectionString;
  private readonly ICurrentUserService? _currentUserService;
  internal VideoContext (string connectionString, ICurrentUserService? currentUserService)
  {
    _connectionString = connectionString;
    _currentUserService = currentUserService;
  }
  protected override void OnConfiguring(DbContextOptionsBuilder options)
  {
    options.UseSqlite(_connectionString);
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<VideoFileWindows>()
      .HasKey(v => new { v.Path, v.Owner });
    modelBuilder.Entity<VideoFileGoogle>()
      .HasKey(v => new { v.GoogleId, v.Owner });
    
    if (_currentUserService is not null)
    {
      modelBuilder.Entity<VideoFileGoogle>()
          .HasQueryFilter(v => v.Owner == _currentUserService.UserId);
      modelBuilder.Entity<VideoFileWindows>()
          .HasQueryFilter(v => v.Owner == _currentUserService.UserId);
    }
  }  

  private DbSet<T> GetVideos<T>() where T : VideoFileBase
  {
    return Set<T>();
  }
  public DbSet<VideoFileGoogle> GetGoogleVideos()
  {
    return GetVideos<VideoFileGoogle>();
  }
  public DbSet<VideoFileWindows> GetWindowsVideos()
  {
    return GetVideos<VideoFileWindows>();
  }

  // not an override of base class RemoveRange because that takes IEnumerable<object>
  public void RemoveRange(IEnumerable<VideoFileBase> videos)
  {
    if (_currentUserService is not null)
    {
      videos = videos.Where(v => v.Owner == _currentUserService.UserId);
    }
    base.RemoveRange(videos);
    SaveChanges();
  }
  public void RemoveAllVideos()
  {
    var videos = GetVideos<VideoFileBase>();
    RemoveRange(videos);
    SaveChanges();
  }

  // hide some methods from the DbContext base class
  public new void Add<T>(T video) where T : VideoFileBase
  {
    if (_currentUserService is not null)
    {
      video.Owner = _currentUserService.UserId;
    }
    base.Add(video);
  }
  
  public new void Update<T>(T video) where T : VideoFileBase
  {
    if (_currentUserService is not null)
    {
      video.Owner = _currentUserService.UserId;
    }    
    base.Update(video);
  }

  public string? GetCurrentUser()
  {
    return _currentUserService?.UserId;
  }
}