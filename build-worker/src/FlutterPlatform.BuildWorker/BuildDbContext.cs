using Microsoft.EntityFrameworkCore;

namespace FlutterPlatform.BuildWorker;

// Minimal EF context for the worker — uses the same DB as the API
public class BuildDbContext : DbContext
{
    public BuildDbContext(DbContextOptions options) : base(options) { }

    public DbSet<BuildRecord> Builds => Set<BuildRecord>();
    public DbSet<ProjectVersionRecord> ProjectVersions => Set<ProjectVersionRecord>();
    public DbSet<ProjectRecord> Projects => Set<ProjectRecord>();
    public DbSet<ProjectFileRecord> ProjectFiles => Set<ProjectFileRecord>();
    public DbSet<BuildLogEntry> BuildLogs => Set<BuildLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BuildRecord>().ToTable("Builds");
        modelBuilder.Entity<ProjectVersionRecord>().ToTable("ProjectVersions");
        modelBuilder.Entity<ProjectRecord>().ToTable("Projects");
        modelBuilder.Entity<ProjectFileRecord>().ToTable("ProjectFiles");
        modelBuilder.Entity<BuildLogEntry>().ToTable("BuildLogs");

        modelBuilder.Entity<BuildRecord>()
            .HasOne(b => b.ProjectVersion)
            .WithMany(pv => pv.Builds)
            .HasForeignKey(b => b.ProjectVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BuildRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ProjectVersionId { get; set; }
    public int BuildNumber { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ArtifactUrl { get; set; }
    public string? Sha256 { get; set; }
    public long? ArtifactSize { get; set; }
    public string? FlutterSdkVersion { get; set; }
    public string? DartSdkVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ProjectVersionRecord? ProjectVersion { get; set; }
}

public class ProjectVersionRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Version { get; set; } = string.Empty;
    public int BuildNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ProjectRecord? Project { get; set; }
    public ICollection<BuildRecord> Builds { get; set; } = new List<BuildRecord>();
}

public class ProjectRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int BuildNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProjectFileRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BuildLogEntry
{
    public Guid Id { get; set; }
    public Guid BuildId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = "info";
    public DateTime Timestamp { get; set; }
}
