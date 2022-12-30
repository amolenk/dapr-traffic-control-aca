namespace FineCollectionService.Database;

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

        // builder.Property(fine => fine.Id)
        //     .IsRequired();

        // builder.Property(fine => fine.Id)
        //     .IsRequired(true)
        //     .HasMaxLength(50);

        // builder.Property(fine => fine.)
        //     .HasPrecision(4, 2)
        //     .IsRequired(true);

        // builder.Property(fine => fine.PictureFileName)
        //     .IsRequired(true);

        // builder.HasOne(fine => fine.CatalogBrand)
        //     .WithMany()
        //     .HasForeignKey(fine => fine.CatalogBrandId);

        // builder.HasOne(item => item.CatalogType)
        //     .WithMany()
        //     .HasForeignKey(fine => fine.CatalogTypeId);
    }
}

    // public string Id { get; private set; }
    // public decimal Amount { get; private set; }
    // public string VehicleId { get; private set; }
    // public string RoadId { get; private set; }
    // public string VehicleBrand { get; private set; }
    // public string VehicleModel { get; private set; }
    // public decimal ViolationInKmh { get; private set; }
    // public DateTime Timestamp { get; private set; }