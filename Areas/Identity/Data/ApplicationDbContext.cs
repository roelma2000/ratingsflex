using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ratingsflex.Areas.Identity.Data;
using ratingsflex.Areas.Movies.Models;

namespace ratingsflex.Areas.Identity.Data;

public class ApplicationDbContext : IdentityDbContext<ratingsflexUser>
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
    }

    public DbSet<Movie> Movies { get; set; } 
    public DbSet<Poster> Posters { get; set; } 
    public class ApplicationUserEntityConfiguration : IEntityTypeConfiguration<ratingsflexUser>
    {
        public void Configure(EntityTypeBuilder<ratingsflexUser> builder)
        {
            builder.Property(u => u.Firstname).HasMaxLength(255);
            builder.Property(u => u.Lastname).HasMaxLength(255);
        }
    }
}
