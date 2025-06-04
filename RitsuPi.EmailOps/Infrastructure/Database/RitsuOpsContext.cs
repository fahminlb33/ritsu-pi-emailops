using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RitsuPi.EmailOps.Domain.Entities;

namespace RitsuPi.EmailOps.Infrastructure.Database;

public class RitsuOpsContext(DbContextOptions<RitsuOpsContext> options) : DbContext(options)
{
    public DbSet<EmailHistory> EmailHistories { get; set; }
    public DbSet<SemanticKernelHistory> MessageThreads { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<EmailDirection>().HaveConversion<EnumToStringConverter<EmailDirection>>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailHistory>(table =>
        {
            table.HasKey(column => column.Id);
        });

        modelBuilder.Entity<SemanticKernelHistory>(table =>
        {
            table.HasKey(column => column.Id);
            table.HasIndex(column => column.ThreadHash).IsUnique();
            table.HasMany(navigation => navigation.EmailHistories)
                .WithOne(navigation => navigation.SemanticKernelHistory)
                .HasForeignKey(column => column.SemanticKernelHistoryId);
        });
    }
}
