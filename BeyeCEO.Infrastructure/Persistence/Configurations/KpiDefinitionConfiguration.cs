using BeyeCEO.Domain.KPIs.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class KpiDefinitionConfiguration : IEntityTypeConfiguration<KpiDefinition>
    {
        public void Configure(EntityTypeBuilder<KpiDefinition> builder)
        {
            builder.ToTable("KpiDefinitions", "config");

            builder.HasKey(e => e.Code);

            builder.Property(e => e.Code)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(e => e.NameEN)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.NameAR)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.DescriptionEN)
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.DescriptionAR)
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.Unit)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.Category)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(e => e.WarningThreshold)
                .HasPrecision(18, 4);

            builder.Property(e => e.AlertThreshold)
                .HasPrecision(18, 4);

            builder.Property(e => e.IsHigherBetter)
                .HasDefaultValue(true);

            builder.Property(e => e.IsActive)
                .HasDefaultValue(true);
        }
    }
}
