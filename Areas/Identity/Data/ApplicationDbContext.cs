using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ratingsflex.Areas.Identity.Data;
using ratingsflex.Areas.Movies.Models;

namespace ratingsflex.Areas.Identity.Data;

public class ApplicationDbContext : IdentityDbContext<RatingsflexUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
        builder.ApplyConfiguration(new ApplicationUserEntityConfiguration());

        // These lines should be inside the OnModelCreating method
        builder.Entity<Movie>().HasKey(m => m.Id);
        builder.Entity<Poster>().HasKey(p => p.Id);
    }

    public DbSet<Movie> Movies { get; set; }
    public DbSet<Poster> Posters { get; set; }
}

public class ApplicationUserEntityConfiguration : IEntityTypeConfiguration<RatingsflexUser>
{
    public void Configure(EntityTypeBuilder<RatingsflexUser> builder)
    {
        builder.Property(u => u.Firstname).HasMaxLength(255);
        builder.Property(u => u.Lastname).HasMaxLength(255);
    }
}
