using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId).HasName("PK_Users");
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.ProfilePictureUrl).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
        });

        modelBuilder.Entity<EventType>(e =>
        {
            e.ToTable("EventTypes");
            e.HasKey(x => x.EventTypeId).HasName("PK_EventTypes");
            e.HasIndex(x => x.TypeName).IsUnique().HasDatabaseName("UQ_EventTypes_TypeName");
            e.Property(x => x.TypeName).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.EventId).HasName("PK_Events");
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Location).HasMaxLength(200).IsRequired();
            e.Property(x => x.DistanceKm).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.BannerImageUrl).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Organiser).WithMany(x => x.Events)
                .HasForeignKey(x => x.OrganiserId).HasConstraintName("FK_Events_Organiser")
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EventType).WithMany(x => x.Events)
                .HasForeignKey(x => x.EventTypeId).HasConstraintName("FK_Events_EventType")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryId).HasName("PK_Categories");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(300);
            e.HasOne(x => x.Event).WithMany(x => x.Categories)
                .HasForeignKey(x => x.EventId).HasConstraintName("FK_Categories_Events")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments");
            e.HasKey(x => x.EnrolmentId).HasName("PK_Enrolments");
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
            e.Property(x => x.EnrolmentDate).HasDefaultValueSql("SYSDATETIME()");
            e.HasIndex(x => new { x.ParticipantId, x.EventId }).IsUnique().HasDatabaseName("UQ_Enrolments_Participant_Event");
            e.HasOne(x => x.Participant).WithMany(x => x.Enrolments)
                .HasForeignKey(x => x.ParticipantId).HasConstraintName("FK_Enrolments_Participant")
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Event).WithMany(x => x.Enrolments)
                .HasForeignKey(x => x.EventId).HasConstraintName("FK_Enrolments_Events")
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(x => x.Enrolments)
                .HasForeignKey(x => x.CategoryId).HasConstraintName("FK_Enrolments_Categories")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultId).HasName("PK_Results");
            e.Property(x => x.CapturedAt).HasDefaultValueSql("SYSDATETIME()");
            e.HasIndex(x => x.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_EnrolmentId");
            e.HasOne(x => x.Enrolment).WithOne(x => x.Result)
                .HasForeignKey<Result>(x => x.EnrolmentId).HasConstraintName("FK_Results_Enrolments")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>().HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser','Participant')");
        modelBuilder.Entity<Event>().HasCheckConstraint("CK_Events_DistanceKm", "[DistanceKm] > 0");
        modelBuilder.Entity<Enrolment>().HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Pending','Confirmed','Cancelled')");
        modelBuilder.Entity<Result>().HasCheckConstraint("CK_Results_FinishPosition", "[FinishPosition] IS NULL OR [FinishPosition] > 0");
    }
}
