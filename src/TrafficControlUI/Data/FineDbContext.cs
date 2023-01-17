namespace TrafficControlUI.Data;

public class FineDbContext : DbContext
{
    public DbSet<Fine> Fines => Set<Fine>();

    public FineDbContext(DbContextOptions<FineDbContext> options)
        : base(options)
    {
        
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseExceptionProcessor();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new FineEntityTypeConfiguration());
    }     
}
