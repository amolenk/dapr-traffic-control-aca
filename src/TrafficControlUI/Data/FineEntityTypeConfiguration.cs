namespace TrafficControlUI.Data;

public class FineEntityTypeConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.ToTable("Fine")
            .HasKey(fine => fine.Id);

        builder.Property(fine => fine.Amount)
            .HasPrecision(6, 2)
            .IsRequired(true);

        builder.Property(fine => fine.ViolationInKmh)
            .HasPrecision(6, 2)
            .IsRequired(true);
    }
}
