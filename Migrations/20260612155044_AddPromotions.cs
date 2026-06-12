using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TitleRu = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, defaultValue: ""),
                    TitleUz = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, defaultValue: ""),
                    Subtitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SubtitleRu = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, defaultValue: ""),
                    SubtitleUz = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, defaultValue: ""),
                    ButtonText = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ButtonTextRu = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: ""),
                    ButtonTextUz = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: ""),
                    ActionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "none"),
                    ActionValue = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CardStyle = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "gold"),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotions_IsPublished_DisplayOrder_CreatedAtUtc",
                table: "promotions",
                columns: new[] { "IsPublished", "DisplayOrder", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_promotions_UpdatedAtUtc",
                table: "promotions",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotions");
        }
    }
}
