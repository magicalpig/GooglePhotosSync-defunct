
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
    var dbFileName = configuration.GetValue<string>("DbFileName") ?? throw new Exception("Could not find a db file name under the 'DbFileName' setting in CommonSettings.json.");
    const int DirectoriesUp = 4;
    var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
      string.Concat(Enumerable.Repeat("../", DirectoriesUp))));
    var connectionString = $"Data Source={solutionRoot}{dbFileName}";
    DbPath = connectionString;
    Console.WriteLine($"Database will be DbPath: {DbPath}");
  }

  protected override void OnConfiguring(DbContextOptionsBuilder options)
  {
    options.UseSqlite(DbPath);
  }
}