using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.HasKey(x => x.UserID);
            e.Property(x => x.FullName).HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Email).HasMaxLength(100).IsUnicode(false);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsUnicode(false);
            e.Property(x => x.Role).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Participant");
            e.Property(x => x.ContactNumber).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.DateRegistered).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_Status", "[Status] IN ('Draft', 'Published', 'Completed')");
                t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')");
            });
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasMaxLength(150).IsUnicode(false);
            e.Property(x => x.EventType).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Run");
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.City).HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.DistanceKM).HasColumnType("decimal(5,2)");
            e.Property(x => x.Description).HasColumnType("varchar(max)");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Draft");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserID).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(50).IsUnicode(false);
            e.Property(x => x.DistanceKM).HasColumnType("decimal(5,2)");
            e.Property(x => x.EntryFee).HasColumnType("decimal(8,2)").HasDefaultValue(0.00m);
            e.Property(x => x.MaxParticipants).HasDefaultValue(100);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Venue>(e =>
        {
            e.ToTable("Venues");
            e.HasKey(x => x.VenueID);
            e.Property(x => x.StartPoint).HasMaxLength(150).IsUnicode(false);
            e.Property(x => x.EndPoint).HasMaxLength(150).IsUnicode(false);
            e.Property(x => x.RouteDescription).HasColumnType("varchar(max)");
            e.Property(x => x.ElevationGainM).HasDefaultValue(0);
            e.HasOne(x => x.Event).WithOne(ev => ev.Venue)
                .HasForeignKey<Venue>(x => x.EventID).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Entry>(e =>
        {
            e.ToTable("Entries", t => t.HasCheckConstraint("CK_Entries_PaymentStatus", "[PaymentStatus] IN ('Pending', 'Paid', 'Refunded')"));
            e.HasKey(x => x.EntryID);
            e.Property(x => x.BibNumber).HasMaxLength(10).IsUnicode(false);
            e.HasIndex(x => x.BibNumber).IsUnique();
            e.Property(x => x.EntryDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.PaymentStatus).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Pending");
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique()
                .HasDatabaseName("UQ_Entries_Participant_Category");
            e.HasOne(x => x.Participant).WithMany(u => u.Entries)
                .HasForeignKey(x => x.ParticipantID).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Entries)
                .HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Result>(e =>
        {
            e.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished', 'DNF', 'DNS')"));
            e.HasKey(x => x.ResultID);
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Finished");
            e.HasOne(x => x.Entry).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EntryID).OnDelete(DeleteBehavior.Restrict);
        });
    }
}