using Microsoft.EntityFrameworkCore;
using FlutterPlatform.Domain.Entities;

namespace FlutterPlatform.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Auth & Authorization
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    // Projects
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectFile> ProjectFiles { get; set; }
    public DbSet<ProjectVersion> ProjectVersions { get; set; }

    // Builds
    public DbSet<Build> Builds { get; set; }
    public DbSet<BuildLog> BuildLogs { get; set; }
    public DbSet<BuildArtifact> BuildArtifacts { get; set; }

    // Releases
    public DbSet<Release> Releases { get; set; }
    public DbSet<ReleaseManifest> ReleaseManifests { get; set; }

    // Devices
    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceGroup> DeviceGroups { get; set; }
    public DbSet<DeviceGroupMembership> DeviceGroupMemberships { get; set; }
    public DbSet<DeviceInstallation> DeviceInstallations { get; set; }

    // Deployments
    public DbSet<Deployment> Deployments { get; set; }
    public DbSet<DeploymentDevice> DeploymentDevices { get; set; }

    // Events
    public DbSet<UpdateEvent> UpdateEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User-Role relationship
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Role-Permission relationship
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Project relationships
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Owner)
            .WithMany(u => u.Projects)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectFile>()
            .HasOne(pf => pf.Project)
            .WithMany(p => p.Files)
            .HasForeignKey(pf => pf.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectVersion>()
            .HasOne(pv => pv.Project)
            .WithMany(p => p.Versions)
            .HasForeignKey(pv => pv.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Build relationships
        modelBuilder.Entity<Build>()
            .HasOne(b => b.Project)
            .WithMany(p => p.Builds)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Build>()
            .HasOne(b => b.ProjectVersion)
            .WithMany(pv => pv.Builds)
            .HasForeignKey(b => b.ProjectVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BuildLog>()
            .HasOne(bl => bl.Build)
            .WithMany(b => b.Logs)
            .HasForeignKey(bl => bl.BuildId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BuildArtifact>()
            .HasOne(ba => ba.Build)
            .WithMany(b => b.Artifacts)
            .HasForeignKey(ba => ba.BuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Release relationships
        modelBuilder.Entity<Release>()
            .HasOne(r => r.Project)
            .WithMany(p => p.Releases)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Release>()
            .HasOne(r => r.Build)
            .WithMany()
            .HasForeignKey(r => r.BuildId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReleaseManifest>()
            .HasOne(rm => rm.Release)
            .WithOne(r => r.Manifest)
            .HasForeignKey<ReleaseManifest>(rm => rm.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Device relationships
        modelBuilder.Entity<DeviceGroupMembership>()
            .HasOne(dgm => dgm.Device)
            .WithMany(d => d.DeviceGroupMemberships)
            .HasForeignKey(dgm => dgm.DeviceIdentifier)
            .HasPrincipalKey(d => d.DeviceIdentifier)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeviceGroupMembership>()
            .HasOne(dgm => dgm.DeviceGroup)
            .WithMany(dg => dg.DeviceGroupMemberships)
            .HasForeignKey(dgm => dgm.DeviceGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeviceInstallation>()
            .HasOne(di => di.Device)
            .WithMany(d => d.Installations)
            .HasForeignKey(di => di.DeviceIdentifier)
            .HasPrincipalKey(d => d.DeviceIdentifier)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeviceInstallation>()
            .HasOne(di => di.Release)
            .WithMany()
            .HasForeignKey(di => di.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Deployment relationships
        modelBuilder.Entity<Deployment>()
            .HasOne(d => d.Release)
            .WithMany(r => r.Deployments)
            .HasForeignKey(d => d.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeploymentDevice>()
            .HasOne(dd => dd.Deployment)
            .WithMany(d => d.DeploymentDevices)
            .HasForeignKey(dd => dd.DeploymentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeploymentDevice>()
            .HasOne(dd => dd.Device)
            .WithMany(d => d.DeploymentDevices)
            .HasForeignKey(dd => dd.DeviceIdentifier)
            .HasPrincipalKey(d => d.DeviceIdentifier)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure UpdateEvent relationships
        modelBuilder.Entity<UpdateEvent>()
            .HasOne(ue => ue.Device)
            .WithMany(d => d.UpdateEvents)
            .HasForeignKey(ue => ue.DeviceIdentifier)
            .HasPrincipalKey(d => d.DeviceIdentifier)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UpdateEvent>()
            .HasOne(ue => ue.Release)
            .WithMany()
            .HasForeignKey(ue => ue.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Release>()
            .Ignore(r => r.Mandatory);

        // Indexes
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.ApplicationId)
            .IsUnique();

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.OwnerId);

        modelBuilder.Entity<Device>()
            .HasAlternateKey(d => d.DeviceIdentifier);

        modelBuilder.Entity<Device>()
            .HasIndex(d => d.DeviceIdentifier)
            .IsUnique();

        modelBuilder.Entity<Device>()
            .HasIndex(d => d.ApplicationId);

        modelBuilder.Entity<Device>()
            .HasIndex(d => d.UpdateChannel);

        modelBuilder.Entity<Build>()
            .HasIndex(b => b.ProjectId);

        modelBuilder.Entity<Build>()
            .HasIndex(b => b.Status);

        modelBuilder.Entity<Release>()
            .HasIndex(r => r.ProjectId);

        modelBuilder.Entity<Release>()
            .HasIndex(r => r.Channel);

        modelBuilder.Entity<Release>()
            .HasIndex(r => r.Status);
    }
}
