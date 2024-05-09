using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VidCommon.Models;
namespace VidCommon;

public class VideoContext : DbContext
{
  public DbSet<VideoFileGoogle> GoogleVideos { get; set; }
  public DbSet<VideoFileWindows> WindowsVideos { get; set; }
  public DbSet<VideoMetadataFromChrome> VideoMetadataFromChrome { get; set; }

  public string DbPath { get; }

  public VideoContext()
  {
    var builder = new ConfigurationBuilder()
      .AddJsonFile("CommonSettings.json", optional: false, reloadOnChange: true);

    var configuration = builder.Build();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (connectionString == null)
    {
      throw new Exception("Could not find a connection string named 'DefaultConnection' in the settings.json file.");
    }
    DbPath = connectionString;
  }

  protected override void OnConfiguring(DbContextOptionsBuilder options)
  {
    options.UseSqlite(DbPath);    
  }
}