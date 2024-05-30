using Microsoft.EntityFrameworkCore;
using VideoDataAccess.Models;

namespace VideoDataAccess.Services;


// A VideoContext should only be created by a VideoContextFactory
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

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<VideoFileWindows>()
      .HasKey(v => new { v.Path, v.Owner });
    modelBuilder.Entity<VideoFileGoogle>()
      .HasKey(v => new { v.GoogleId, v.Owner });
  }  
}