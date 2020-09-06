using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace KeyKeeperApi.Common.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "key_keeper_api");

            migrationBuilder.CreateTable(
                name: "blockchains",
                schema: "key_keeper_api",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    TenantId = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Protocol = table.Column<string>(nullable: true),
                    NetworkType = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blockchains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transaction_approval_requests",
                schema: "key_keeper_api",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false),
                    KeyKeeperId = table.Column<long>(nullable: false),
                    TenantId = table.Column<string>(nullable: true),
                    VaultId = table.Column<long>(nullable: false),
                    TransactionSigningRequestId = table.Column<long>(nullable: false),
                    VaultName = table.Column<string>(nullable: true),
                    BlockchainId = table.Column<string>(nullable: true),
                    BlockchainName = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    Secret = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_approval_requests", x => new { x.Id, x.KeyKeeperId });
                });

            migrationBuilder.CreateTable(
                name: "vaults",
                schema: "key_keeper_api",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    TenantId = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vaults", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_approval_requests_KeyKeeperId",
                schema: "key_keeper_api",
                table: "transaction_approval_requests",
                column: "KeyKeeperId");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_approval_requests_Status",
                schema: "key_keeper_api",
                table: "transaction_approval_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_vaults_TenantId",
                schema: "key_keeper_api",
                table: "vaults",
                column: "TenantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blockchains",
                schema: "key_keeper_api");

            migrationBuilder.DropTable(
                name: "transaction_approval_requests",
                schema: "key_keeper_api");

            migrationBuilder.DropTable(
                name: "vaults",
                schema: "key_keeper_api");
        }
    }
}
