using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
     public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Define DbSet properties for your entities
    public DbSet<Admin>? Admins { get; set; }
}