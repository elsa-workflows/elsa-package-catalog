using System.Text.Json;
using Elsa.Catalog.Core.Manifests;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore.Models;

internal sealed class PackageSourceConfiguration : IEntityTypeConfiguration<PackageSource>
{
    private static readonly ValueComparer<List<string>> StringListComparer = new(
        (left, right) => ReferenceEquals(left, right) || (left != null && right != null && left.SequenceEqual(right)),
        value => value == null ? 0 : value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode(StringComparison.Ordinal))),
        value => value == null ? new List<string>() : value.ToList());

    public void Configure(EntityTypeBuilder<PackageSource> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.LastSyncError).HasMaxLength(2048);
        builder.Property(x => x.PollingInterval).HasMaxLength(64);
        builder.Property(x => x.VersionDiscoveryPolicy).HasDefaultValue(PackageSourceVersionDiscoveryPolicy.AllVersions);
        builder.Property(x => x.IncludePatterns)
            .HasConversion(value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), value => JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(StringListComparer);
        builder.Property(x => x.ExcludePatterns)
            .HasConversion(value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), value => JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(StringListComparer);
        builder.HasMany(x => x.Packages).WithOne(x => x.Source).HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PackageId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.SourceId, x.PackageId }).IsUnique();
        builder.HasMany(x => x.Versions).WithOne(x => x.Package).HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PackageVersionConfiguration : IEntityTypeConfiguration<PackageVersion>
{
    public void Configure(EntityTypeBuilder<PackageVersion> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ManifestJson).IsRequired();
        builder.Property(x => x.ManifestHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.PackageId, x.Version }).IsUnique();
        builder.HasMany(x => x.Features).WithOne(x => x.PackageVersion).HasForeignKey(x => x.PackageVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class FeatureRecordConfiguration : IEntityTypeConfiguration<FeatureRecord>
{
    public void Configure(EntityTypeBuilder<FeatureRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FeatureId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.TypeName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.PackageVersionId, x.FeatureId }).IsUnique();
        builder.HasMany(x => x.Settings).WithOne(x => x.FeatureRecord).HasForeignKey(x => x.FeatureRecordId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class FeatureSettingRecordConfiguration : IEntityTypeConfiguration<FeatureSettingRecord>
{
    public void Configure(EntityTypeBuilder<FeatureSettingRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.JsonType).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.FeatureRecordId, x.Name }).IsUnique();
    }
}

internal sealed class ManifestValidationResultRecordConfiguration : IEntityTypeConfiguration<ManifestValidationResultRecord>
{
    public void Configure(EntityTypeBuilder<ManifestValidationResultRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.PackageVersion).WithMany().HasForeignKey(x => x.PackageVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ApprovalRecordConfiguration : IEntityTypeConfiguration<ApprovalRecord>
{
    public void Configure(EntityTypeBuilder<ApprovalRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Actor).HasMaxLength(256).IsRequired();
    }
}

internal sealed class SyncRunConfiguration : IEntityTypeConfiguration<SyncRun>
{
    public void Configure(EntityTypeBuilder<SyncRun> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StartedAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
        builder.Property(x => x.CompletedAt)
            .HasConversion(value => value.HasValue ? value.Value.UtcTicks : (long?)null, value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);
        builder.HasMany(x => x.Items).WithOne(x => x.SyncRun).HasForeignKey(x => x.SyncRunId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SyncRunItemConfiguration : IEntityTypeConfiguration<SyncRunItem>
{
    public void Configure(EntityTypeBuilder<SyncRunItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.PackageVersion).WithMany().HasForeignKey(x => x.PackageVersionId).OnDelete(DeleteBehavior.SetNull);
    }
}
