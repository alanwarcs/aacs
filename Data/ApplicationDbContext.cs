using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
     public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Define DbSet properties for your entities
    public DbSet<Admin>? Admins { get; set; }
    public DbSet<Service>? Service { get; set; }
    public DbSet<Blog>? Blog { get; set; }

    public DbSet<Contact>? Contact { get; set; }
}