using System.Text.Json;
using CityConsumer.Models;
using Microsoft.EntityFrameworkCore;

namespace CityConsumer.Context
{
    public class ConsumerContext : DbContext
    {
        public ConsumerContext()
        {

        }

        public ConsumerContext(DbContextOptions<ConsumerContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Name=ConnectionStrings:Default");
            }
        }

        public DbSet<Events> Events { get; set; }
        public DbSet<Alerts> Alerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Events>(entity =>
            {
                entity.ToTable("events");

                entity.HasKey(x => x.EventId);

                entity.HasIndex(x => x.TsUtc);
                entity.HasIndex(x => x.EventType);
                entity.HasIndex(x => x.Zone);
                entity.HasIndex(x => x.PartitionKey);

                entity.Property(x => x.EventId)
                    .HasMaxLength(100)
                    .HasColumnName("event_id");
                entity.Property(x => x.EventType)
                    .HasMaxLength(250)
                    .HasColumnName("event_type");
                entity.Property(x => x.EventVersion)
                    .HasMaxLength(200)
                    .HasColumnName("event_version");
                entity.Property(x => x.Producer)
                    .HasMaxLength(100)
                    .HasColumnName("producer");
                entity.Property(x => x.Source)
                    .HasMaxLength(100)
                    .HasColumnName("source");
                entity.Property(x => x.CorrelationId)
                    .HasMaxLength(100)
                    .HasColumnName("correlation_id");
                entity.Property(x => x.TraceId)
                    .HasMaxLength(100)
                    .HasColumnName("trace_id");
                entity.Property(x => x.PartitionKey)
                    .HasMaxLength(100)
                    .HasColumnName("partition_key");
                entity.Property(x => x.Severity)
                    .HasMaxLength(100)
                    .HasColumnName("severity");
                entity.Property(x => x.TsUtc)
                    .HasColumnName("ts_utc");
                entity.Property(x => x.GeoLat)
                    .HasColumnName("geo_lat");
                entity.Property(x => x.GeoLong)
                    .HasColumnName("geo_long");
                entity.Property(x => x.Zone)
                    .HasMaxLength(500)
                    .HasColumnName("zone");
                entity.Property(e => e.Payload)
                    .HasColumnName("payload")
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => v!.RootElement.GetRawText(),
                        v => JsonDocument.Parse(v, new JsonDocumentOptions())
                    );
            });

            modelBuilder.Entity<Alerts>(entity =>
            {
                entity.ToTable("alerts");

                entity.HasKey(x => x.AlertId);
                entity.HasIndex(x => x.CreatedAt);
                entity.HasIndex(x => x.Zone);

                entity.Property(x => x.Zone)
                    .HasMaxLength(500)
                    .HasColumnName("zone");
                entity.Property(x => x.AlertId)
                    .HasMaxLength(100)
                    .HasColumnName("alert_id");
                entity.Property(x => x.CorrelationId)
                    .HasMaxLength(100)
                    .HasColumnName("correlation_id");
                entity.Property(x => x.Type)
                    .HasMaxLength(100)
                    .HasColumnName("type");
                entity.Property(x => x.Score)
                    .HasColumnName("score");
                entity.Property(x => x.WindowStart)
                    .HasColumnName("window_start");
                entity.Property(x => x.WindowEnd)
                    .HasColumnName("window_end");
                entity.Property(x => x.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.Evidence)
                    .HasColumnName("evidence")
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => v!.RootElement.GetRawText(),
                        v => JsonDocument.Parse(v, new JsonDocumentOptions())
                    );
            });
        }
    }
}
