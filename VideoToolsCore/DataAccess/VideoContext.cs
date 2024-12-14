using Microsoft.EntityFrameworkCore;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.User.Services;

namespace VideoToolsCore.DataAccess;


// A VideoContext should only be created by a VideoContextFactory: internal constructor
public class VideoContext : DbContext
{
    public DbSet<VideoFileGoogle> GoogleVideos => GetVideos<VideoFileGoogle>();
    public DbSet<VideoFileWindows> WindowsVideos => GetVideos<VideoFileWindows>();
    public DbSet<GoogleAlbum> GoogleAlbums => GetAlbums<GoogleAlbum>();
    public DbSet<AlbumMembership> AlbumMemberships => GetAlbumMemberships();

    private readonly string _connectionString;
    private readonly IDualCurrentUserService? _currentUserService;

    internal VideoContext(string connectionString, IDualCurrentUserService? currentUserService)
    {
        _connectionString = connectionString;
        _currentUserService = currentUserService;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite(_connectionString)
            //.LogTo(Console.WriteLine, LogLevel.Information)
            ;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VideoFileWindows>()
            .HasKey(v => new { v.Path, v.Owner });
        modelBuilder.Entity<VideoFileGoogle>()
            .HasKey(v => new { v.RestId, v.Owner });
        modelBuilder.Entity<GoogleAlbum>()
            .HasKey(a => new { a.Id, a.Owner });
        modelBuilder.Entity<AlbumMembership>()
            .HasKey(am => new { am.VideoId, am.AlbumId });

        // Lets EF Migrations see these 2 internal members
        modelBuilder.Entity<VideoFileWindows>()
            .Property(v => v.MachineName)
            .HasColumnName("MachineName");
        modelBuilder.Entity<VideoFileWindows>()
            .Property(v => v.MachineId)
            .HasColumnName("MachineId");

        // query filter is applied on all selects, updates, and deletes.
        if (_currentUserService is not null)
        {
            var googleUser = _currentUserService.GoogleCurrentUserService?.UserId;
            var localUser = _currentUserService.LocalCurrentUserService?.UserId;
            modelBuilder.Entity<VideoFileGoogle>()
                .HasQueryFilter(v => v.Owner == googleUser);
            modelBuilder.Entity<GoogleAlbum>()
                .HasQueryFilter(a => a.Owner == googleUser);
            modelBuilder.Entity<AlbumMembership>()
                .HasQueryFilter(am => am.Owner == googleUser);
            modelBuilder.Entity<VideoFileWindows>()
                .HasQueryFilter(v => v.Owner == localUser && v.MachineId == LocalMachineIdService.GetUUID());
        }
        else
        {
            modelBuilder.Entity<VideoFileWindows>()
                .HasQueryFilter(v => v.MachineId == LocalMachineIdService.GetUUID());
        }

        // Relationship between Google videos and albums
        modelBuilder.Entity<AlbumMembership>()
            .HasOne(am => am.VideoFileGoogle)
            .WithMany(vfg => vfg.AlbumMemberships)
            .HasForeignKey(am => new { am.VideoId, am.Owner })
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AlbumMembership>()
            .HasOne(am => am.GoogleAlbum)
            .WithMany(ga => ga.AlbumMemberships)
            .HasForeignKey(am => new { am.AlbumId, am.Owner })
            .OnDelete(DeleteBehavior.Cascade);

        // make Name properties case-insensitive
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
    private DbSet<T> GetAlbums<T>() where T : GoogleAlbum // maybe will be expanded to other types of albums
    {
        return Set<T>();
    }
    private DbSet<AlbumMembership> GetAlbumMemberships()
    {
        return Set<AlbumMembership>();
    }


    public void RemoveAllVideos()
    {
        var videos = GetVideos<VideoFileBase>();
        RemoveRange(videos);
        SaveChanges();
    }

    // hide some methods from the DbContext base class
    public new void Add<T>(T entity) where T : OwnableEntityBase
    {
        base.Add(AttachMachineIdAndName(AttachOwner(entity)));
    }

    public new void Update<T>(T entity) where T : OwnableEntityBase
    {
        base.Update(AttachMachineIdAndName(AttachOwner(entity)));
    }

    private OwnableEntityBase AttachOwner(OwnableEntityBase entity)
    {
        if (_currentUserService is not null)
        {
            var entry = Entry(entity); // converting to EntityEntry helps DbContext track changes
            var owner = _currentUserService.GetCurrentUserForEntity(entity);

            entry.Property("Owner").CurrentValue = owner;
        }
        return entity;
    }

    private object AttachMachineIdAndName(object entity)
    {
        if (entity is VideoFileWindows videoFileWindows)
        {
            videoFileWindows.MachineId = LocalMachineIdService.GetUUID() ?? string.Empty;
            videoFileWindows.MachineName = LocalMachineIdService.GetName() ?? string.Empty;
        }
        return entity;
    }
}