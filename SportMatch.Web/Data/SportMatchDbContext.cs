using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SportMatch.Web.Data.Entities;

namespace SportMatch.Web.Data;

public sealed class SportMatchDbContext(DbContextOptions<SportMatchDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<VenueComplex> VenueComplexes => Set<VenueComplex>();
    public DbSet<SportCourt> SportCourts => Set<SportCourt>();
    public DbSet<CourtTimeSlot> CourtTimeSlots => Set<CourtTimeSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<MatchPost> MatchPosts => Set<MatchPost>();
    public DbSet<MatchJoinRequest> MatchJoinRequests => Set<MatchJoinRequest>();
    public DbSet<PaymentWebhookLog> PaymentWebhookLogs => Set<PaymentWebhookLog>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Booking>().HasIndex(x => x.BookingCode).IsUnique();
        builder.Entity<Booking>().HasIndex(x => x.PaymentTransactionId).IsUnique().HasFilter("[PaymentTransactionId] IS NOT NULL");
        builder.Entity<Booking>().Property(x => x.TotalAmount).HasPrecision(18, 0);
        builder.Entity<Booking>().Property(x => x.DepositAmount).HasPrecision(18, 0);
        builder.Entity<Booking>().Property(x => x.RefundAmount).HasPrecision(18, 0);
        builder.Entity<Booking>().Property(x => x.MatchCostPerPerson).HasPrecision(18, 0);
        builder.Entity<SportCourt>().Property(x => x.OffPeakPrice).HasPrecision(18, 0);
        builder.Entity<SportCourt>().Property(x => x.PeakPrice).HasPrecision(18, 0);
        builder.Entity<ApplicationUser>().Property(x => x.TrustScore).HasPrecision(3, 1);
        builder.Entity<MatchPost>().HasIndex(x => x.MatchCode).IsUnique();
        builder.Entity<MatchPost>().HasIndex(x => x.SourceBookingId).IsUnique().HasFilter("[SourceBookingId] IS NOT NULL");
        builder.Entity<MatchPost>().HasOne(x => x.SourceBooking).WithOne(x => x.MatchPost).HasForeignKey<MatchPost>(x => x.SourceBookingId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<MatchPost>().Property(x => x.CostPerPerson).HasPrecision(18, 0);
        builder.Entity<MatchJoinRequest>().HasIndex(x => x.RequestCode).IsUnique();
        builder.Entity<MatchJoinRequest>().HasIndex(x => x.PaymentTransactionId).IsUnique().HasFilter("[PaymentTransactionId] IS NOT NULL");
        builder.Entity<MatchJoinRequest>().Property(x => x.DepositAmount).HasPrecision(18, 0);
        builder.Entity<PaymentWebhookLog>().HasIndex(x => x.TransactionId);
        builder.Entity<PaymentWebhookLog>().Property(x => x.Amount).HasPrecision(18, 0);
        builder.Entity<AdminAuditLog>().HasIndex(x => x.OccurredAtUtc);
    }
}
