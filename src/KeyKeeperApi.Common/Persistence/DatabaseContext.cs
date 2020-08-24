using KeyKeeperApi.Common.ReadModels.Blockchains;
using KeyKeeperApi.Common.ReadModels.TransactionApprovalRequests;
using KeyKeeperApi.Common.ReadModels.Vaults;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace KeyKeeperApi.Common.Persistence
{
    public class DatabaseContext : DbContext
    {
        private static readonly JsonSerializerSettings JsonSerializingSettings =
            new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};

        public static string SchemaName { get; } = "key_keeper_api";

        public static string MigrationHistoryTable { get; } = "__EFMigrationsHistory";

        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        public DbSet<Blockchain> Blockchains { get; set; }

        public DbSet<TransactionApprovalRequest> TransactionApprovalRequests { get; set; }

        public DbSet<Vault> Vaults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);

            BuildBlockchain(modelBuilder);
            BuildTransactionApprovalRequest(modelBuilder);
            BuildVaults(modelBuilder);
        }

        private static void BuildBlockchain(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Blockchain>()
                .ToTable("blockchains")
                .HasKey(entity => entity.Id);

            modelBuilder.Entity<Blockchain>()
                .Property(e => e.Protocol)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v, JsonSerializingSettings),
                    v => JsonConvert.DeserializeObject<Protocol>(v, JsonSerializingSettings));
        }

        private static void BuildTransactionApprovalRequest(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransactionApprovalRequest>()
                .ToTable("transaction_approval_requests")
                .HasKey(entity =>
                    new
                    {
                        entity.Id,
                        entity.KeyKeeperId
                    });

            modelBuilder.Entity<TransactionApprovalRequest>()
                .HasIndex(entity => entity.KeyKeeperId);

            modelBuilder.Entity<TransactionApprovalRequest>()
                .HasIndex(entity => entity.Status);
        }

        private static void BuildVaults(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Vault>()
                .ToTable("vaults")
                .HasKey(entity => entity.Id);

            modelBuilder.Entity<Vault>()
                .HasIndex(entity => entity.TenantId);
        }
    }
}
