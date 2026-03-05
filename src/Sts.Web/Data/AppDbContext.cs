using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sts.Web.Models;

namespace Sts.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .Property(u => u.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Entity<ApplicationUser>()
            .Property(u => u.Team)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Entity<Ticket>(entity =>
        {
            entity.Property(t => t.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(t => t.Description)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
