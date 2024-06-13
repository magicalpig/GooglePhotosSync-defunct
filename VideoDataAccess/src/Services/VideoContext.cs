using Microsoft.EntityFrameworkCore;
using VideoDataAccess.Models;

namespace VideoDataAccess.Services;


// A VideoContext should only be created by a VideoContextFactory: internal constructor
public class VideoContext : DbContext
{
  public DbSet<VideoFileGoogle> GoogleVideos => GetVideos<VideoFileGoogle>();
  public DbSet<VideoFileWindows> WindowsVideos => GetVideos<VideoFileWindows>();
  public DbSet<GoogleAlbum> GoogleAlbums { get; set; }
  public DbSet<AlbumMembership> AlbumMemberships { get; set; }

  private readonly string _connectionString;
  private readonly ICurrentUserService? _currentUserService;
  internal VideoContext(string connectionString, ICurrentUserService? currentUserService)
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
    modelBuilder.Entity<GoogleAlbum>()
      .HasKey(a => new { a.Id, a.Owner });
    
    if (_currentUserService is not null)
    {
      modelBuilder.Entity<VideoFileGoogle>()
        .HasQueryFilter(v => v.Owner == _currentUserService.UserId);
      modelBuilder.Entity<VideoFileWindows>()
        .HasQueryFilter(v => v.Owner == _currentUserService.UserId);
      modelBuilder.Entity<GoogleAlbum>()
        .HasQueryFilter(a => a.Owner == _currentUserService.UserId);
      modelBuilder.Entity<AlbumMembership>()
        .HasQueryFilter(am => am.Owner == _currentUserService.UserId);
    }

    modelBuilder.Entity<AlbumMembership>()
      .HasKey(am => new { am.VideoFileGoogleId, am.GoogleAlbumId });
    modelBuilder.Entity<AlbumMembership>()
      .HasOne(am => am.VideoFileGoogle)
      .WithMany(vfg => vfg.AlbumMemberships)
      .HasForeignKey(am => new { am.VideoFileGoogleId, am.Owner });
    modelBuilder.Entity<AlbumMembership>()
      .HasOne(am => am.GoogleAlbum)
      .WithMany(ga => ga.AlbumMemberships)
      .HasForeignKey(am => new { am.GoogleAlbumId, am.Owner });

    // make Name propeties case-insensitive
    modelBuilder.Entity<VideoFileGoogle>()
      .Property(v => v.Name)
      .UseCollation("NOCASE");
    modelBuilder.Entity<VideoFileWindows>()
      .Property(v => v.Name)
      .UseCollation("NOCASE");
  }  

  private DbSet<T> GetVideos<T>() where T : VideoFileBase
  {
    return Set<T>();
  }

  // not an override of base class RemoveRange because that takes IEnumerable<object>
  public void RemoveRange(IEnumerable<VideoFileBase> videos)
  {
    if (_currentUserService is not null)
    {
      videos = videos.Where(v => v.Owner == _currentUserService.UserId);
    }

    // remove album associations
    foreach (var video in videos.OfType<VideoFileGoogle>())
    {
      var albumMemberships = AlbumMemberships
        .Where(am => am.VideoFileGoogleId == video.GoogleId)
        .ToList();

      RemoveRange(albumMemberships);
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
  public new void Add<T>(T entity) where T : OwnableBase
  {    
    base.Add(AttachOwner(entity));
  }
  
  public new void Update<T>(T entity) where T : OwnableBase
  {
    base.Update(AttachOwner(entity));
  }

  private OwnableBase AttachOwner(OwnableBase entity)
  {
    if (_currentUserService is not null)
    {
      var entry = Entry(entity); // converting to EntityEntry helps DbContext track changes
      entry.Property("Owner").CurrentValue = _currentUserService.UserId;
    }
    return entity;
  }

  public string? GetCurrentUser()
  {
    return _currentUserService?.UserId;
  }
}