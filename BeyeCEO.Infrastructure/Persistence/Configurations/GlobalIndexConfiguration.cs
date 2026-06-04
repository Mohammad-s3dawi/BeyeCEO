using BeyeCEO.Domain.MarketData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class GlobalIndexConfiguration : IEntityTypeConfiguration<GlobalIndex>
    {
        public void Configure(EntityTypeBuilder<GlobalIndex> builder)
        {
            builder.ToTable("GlobalIndices", "data");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.Symbol)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Value)
                .HasPrecision(18, 4);

            builder.Property(e => e.Change)
                .HasPrecision(18, 4)
                .HasDefaultValue(0m);

            builder.Property(e => e.ChangePct)
                .HasPrecision(8, 4)
                .HasDefaultValue(0m);

            builder.Property(e => e.Region)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.Source)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            builder.HasIndex(e => new { e.Symbol, e.RecordedAt })
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
