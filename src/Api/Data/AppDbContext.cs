using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Data;

/// <summary>
/// The main Entity Framework Core DbContext for the Location Management application.
/// Provides access to all domain entities and configures spatial support via NetTopologySuite.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the database connection and options.
    /// Enables NetTopologySuite spatial support for SQL Server.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // NetTopologySuite spatial support is configured via the DbContextOptions
        // passed to the constructor, typically in Program.cs via:
        // .UseSqlServer(connectionString, x => x.UseNetTopologySuite())
    }

    /// <summary>Gets or sets the Users DbSet.</summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>Gets or sets the Locations DbSet.</summary>
    public DbSet<LocationManagement.Api.Models.Entities.Location> Locations { get; set; } = null!;

    /// <summary>Gets or sets the PendingEdits DbSet.</summary>
    public DbSet<PendingEdit> PendingEdits { get; set; } = null!;

    /// <summary>Gets or sets the LocationCollections DbSet.</summary>
    public DbSet<LocationCollection> LocationCollections { get; set; } = null!;

    /// <summary>Gets or sets the CollectionMembers DbSet.</summary>
    public DbSet<CollectionMember> CollectionMembers { get; set; } = null!;

    /// <summary>Gets or sets the PendingMembershipRequests DbSet.</summary>
    public DbSet<PendingMembershipRequest> PendingMembershipRequests { get; set; } = null!;

    /// <summary>Gets or sets the NamedShapes DbSet.</summary>
    public DbSet<NamedShape> NamedShapes { get; set; } = null!;

    /// <summary>Gets or sets the Images DbSet.</summary>
    public DbSet<Image> Images { get; set; } = null!;

    /// <summary>Gets or sets the Notifications DbSet.</summary>
    public DbSet<Notification> Notifications { get; set; } = null!;

    /// <summary>Gets or sets the AuditEvents DbSet.</summary>
    public DbSet<AuditEvent> AuditEvents { get; set; } = null!;

    /// <summary>Gets or sets the PasswordResetTokens DbSet.</summary>
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

    /// <summary>
    /// Configures the model for the database.
    /// Applies entity configurations and other model customizations.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique case-insensitive indexes
            entity.HasIndex(e => e.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username_Unique");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasIndex(e => e.DisplayName)
                .IsUnique()
                .HasDatabaseName("IX_Users_DisplayName_Unique");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email_Unique");
            entity.Property(e => e.Email)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            // Foreign key to Avatar Image (optional, set null on delete to avoid cascade cycles)
            entity.HasOne(e => e.AvatarImage)
                .WithMany()
                .HasForeignKey(e => e.AvatarImageId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-Many: User → Locations (CreatorId)
            entity.HasMany(e => e.Locations)
                .WithOne(l => l.Creator)
                .HasForeignKey(l => l.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: User → LocationCollections (OwnerId)
            entity.HasMany(e => e.LocationCollections)
                .WithOne(lc => lc.Owner)
                .HasForeignKey(lc => lc.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: User → PendingEdits (SubmittedByUserId)
            entity.HasMany(e => e.PendingEdits)
                .WithOne(pe => pe.SubmittedByUser)
                .HasForeignKey(pe => pe.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: User → Notifications (UserId)
            entity.HasMany(e => e.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: User → AuditEvents (ActingUserId, optional, no cascade)
            entity.HasMany(e => e.AuditEvents)
                .WithOne(ae => ae.ActingUser)
                .HasForeignKey(ae => ae.ActingUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // One-to-Many: User → PasswordResetTokens (UserId)
            entity.HasMany(e => e.PasswordResetTokens)
                .WithOne(prt => prt.User)
                .HasForeignKey(prt => prt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: User → Images (UploadedByUserId)
            entity.HasMany(e => e.UploadedImages)
                .WithOne(i => i.UploadedByUser)
                .HasForeignKey(i => i.UploadedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: User → PendingMembershipRequests (RequestedByUserId)
            entity.HasMany(e => e.PendingMembershipRequests)
                .WithOne(pmr => pmr.RequestedByUser)
                .HasForeignKey(pmr => pmr.RequestedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: User → NamedShapes (CreatedByUserId)
            entity.HasMany(e => e.NamedShapes)
                .WithOne(ns => ns.CreatedByUser)
                .HasForeignKey(ns => ns.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Location entity
        modelBuilder.Entity<LocationManagement.Api.Models.Entities.Location>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Spatial index on Coordinates (GEOGRAPHY type) - created via raw SQL in migration`n            // EF Core cannot create spatial indexes directly, so we skip the index here

            // Foreign key to Creator (required, cascade delete)
            entity.HasOne(e => e.Creator)
                .WithMany(u => u.Locations)
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: Location → PendingEdits (cascade delete)
            entity.HasMany(e => e.PendingEdits)
                .WithOne(pe => pe.Location)
                .HasForeignKey(pe => pe.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: Location → CollectionMembers (cascade delete)
            entity.HasMany(e => e.CollectionMembers)
                .WithOne(cm => cm.Location)
                .HasForeignKey(cm => cm.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-Many: Location ↔ Images (via LocationImages join table)
            // Use NO ACTION on the Location side to avoid cascade delete cycles
            entity.HasMany(e => e.Images)
                .WithMany()
                .UsingEntity(
                    "LocationImages",
                    l => l.HasOne(typeof(Image)).WithMany().HasForeignKey("ImagesId").OnDelete(DeleteBehavior.Cascade),
                    r => r.HasOne(typeof(LocationManagement.Api.Models.Entities.Location)).WithMany().HasForeignKey("LocationId").OnDelete(DeleteBehavior.NoAction));

            // One-to-Many: Location → PendingMembershipRequests (cascade delete)
            entity.HasMany(e => e.PendingMembershipRequests)
                .WithOne(pmr => pmr.Location)
                .HasForeignKey(pmr => pmr.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Name).HasMaxLength(200);

            // Configure ContentSequence JSON serialisation via HasConversion
            entity.Property(e => e.ContentSequence)
                .HasConversion(
                    v => v,
                    v => v ?? "[]",
                    new ValueComparer<string>(
                        (l, r) => l == r,
                        v => v.GetHashCode(),
                        v => v));
        });

        // Configure PendingEdit entity
        modelBuilder.Entity<PendingEdit>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique constraint on (LocationId, SubmittedByUserId)
            entity.HasIndex(e => new { e.LocationId, e.SubmittedByUserId })
                .IsUnique()
                .HasDatabaseName("IX_PendingEdits_LocationId_SubmittedByUserId_Unique");

            // Spatial index on Coordinates (GEOGRAPHY type) - created via raw SQL in migration`n            // EF Core cannot create spatial indexes directly, so we skip the index here

            // Foreign key to Location (required, cascade delete)
            entity.HasOne(e => e.Location)
                .WithMany(l => l.PendingEdits)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to SubmittedByUser (required, no cascade to avoid cycles)
            entity.HasOne(e => e.SubmittedByUser)
                .WithMany(u => u.PendingEdits)
                .HasForeignKey(e => e.SubmittedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Many-to-Many: PendingEdit ↔ Images (via PendingEditImages join table)
            // Use NO ACTION on the PendingEdit side to avoid cascade delete cycles
            entity.HasMany(e => e.Images)
                .WithMany()
                .UsingEntity(
                    "PendingEditImages",
                    l => l.HasOne(typeof(Image)).WithMany().HasForeignKey("ImagesId").OnDelete(DeleteBehavior.Cascade),
                    r => r.HasOne(typeof(PendingEdit)).WithMany().HasForeignKey("PendingEditId").OnDelete(DeleteBehavior.NoAction));

            entity.Property(e => e.Name).HasMaxLength(200);

            // Configure ContentSequence JSON serialisation via HasConversion
            entity.Property(e => e.ContentSequence)
                .HasConversion(
                    v => v,
                    v => v ?? "[]",
                    new ValueComparer<string>(
                        (l, r) => l == r,
                        v => v.GetHashCode(),
                        v => v));
        });

        // Configure LocationCollection entity
        modelBuilder.Entity<LocationCollection>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Foreign key to Owner (required, no cascade to avoid cycles)
            entity.HasOne(e => e.Owner)
                .WithMany(u => u.LocationCollections)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Foreign key to ThumbnailImage (optional)
            entity.HasOne(e => e.ThumbnailImage)
                .WithMany()
                .HasForeignKey(e => e.ThumbnailImageId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to BoundingShape (optional)
            entity.HasOne(e => e.BoundingShape)
                .WithMany(ns => ns.Collections)
                .HasForeignKey(e => e.BoundingShapeId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-Many: LocationCollection → CollectionMembers (cascade delete)
            entity.HasMany(e => e.Members)
                .WithOne(cm => cm.Collection)
                .HasForeignKey(cm => cm.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: LocationCollection → PendingMembershipRequests (cascade delete)
            entity.HasMany(e => e.PendingMembershipRequests)
                .WithOne(pmr => pmr.Collection)
                .HasForeignKey(pmr => pmr.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        // Configure CollectionMember entity
        modelBuilder.Entity<CollectionMember>(entity =>
        {
            // Composite primary key on (LocationId, CollectionId)
            entity.HasKey(e => new { e.LocationId, e.CollectionId });

            // Foreign key to Location (required, cascade delete)
            entity.HasOne(e => e.Location)
                .WithMany(l => l.CollectionMembers)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to LocationCollection (required, cascade delete)
            entity.HasOne(e => e.Collection)
                .WithMany(lc => lc.Members)
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PendingMembershipRequest entity
        modelBuilder.Entity<PendingMembershipRequest>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Foreign key to LocationCollection (required, cascade delete)
            entity.HasOne(e => e.Collection)
                .WithMany(lc => lc.PendingMembershipRequests)
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to RequestedByUser (required, no cascade to avoid cycles)
            entity.HasOne(e => e.RequestedByUser)
                .WithMany(u => u.PendingMembershipRequests)
                .HasForeignKey(e => e.RequestedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Foreign key to Location (required, cascade delete)
            entity.HasOne(e => e.Location)
                .WithMany(l => l.PendingMembershipRequests)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure NamedShape entity
        modelBuilder.Entity<NamedShape>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique case-insensitive index on Name
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_NamedShapes_Name_Unique");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");

            // Spatial index on Geometry (GEOGRAPHY type) - created via raw SQL in migration`n            // EF Core cannot create spatial indexes directly, so we skip the index here

            // Foreign key to CreatedByUser (required, no cascade to avoid cycles)
            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.NamedShapes)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // One-to-Many: NamedShape → LocationCollections (BoundingShapeId)
            entity.HasMany(e => e.Collections)
                .WithOne(lc => lc.BoundingShape)
                .HasForeignKey(lc => lc.BoundingShapeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Image entity
        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Foreign key to UploadedByUser (required, no cascade to avoid cycles)
            entity.HasOne(e => e.UploadedByUser)
                .WithMany(u => u.UploadedImages)
                .HasForeignKey(e => e.UploadedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Property(e => e.FileName).HasMaxLength(500);
            entity.Property(e => e.MimeType).HasMaxLength(50);
            entity.Property(e => e.AltText).HasMaxLength(500);
        });

        // Configure Notification entity
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Foreign key to User (required, cascade delete)
            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditEvent entity
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Foreign key to ActingUser (optional, no cascade - preserve audit trail)
            entity.HasOne(e => e.ActingUser)
                .WithMany(u => u.AuditEvents)
                .HasForeignKey(e => e.ActingUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.ResourceType).HasMaxLength(50);
        });

        // Configure PasswordResetToken entity
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Foreign key to User (required, cascade delete)
            entity.HasOne(e => e.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.TokenHash).HasMaxLength(64);
        });
    }
}

