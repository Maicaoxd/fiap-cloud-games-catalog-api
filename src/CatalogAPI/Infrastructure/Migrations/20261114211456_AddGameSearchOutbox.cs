using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSearchOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameSearchOutbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSearchOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSearchOutbox_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameSearchOutbox_GameId",
                table: "GameSearchOutbox",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSearchOutbox_ProcessedAt_NextAttemptAt_LeaseUntil",
                table: "GameSearchOutbox",
                columns: new[] { "ProcessedAt", "NextAttemptAt", "LeaseUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameSearchOutbox");
        }
    }
}
