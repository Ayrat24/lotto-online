using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizationAndUserPreferredLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "users",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "localization_texts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EnglishValue = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    RussianValue = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    UzbekValue = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_localization_texts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_localization_texts_Key",
                table: "localization_texts",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_localization_texts_UpdatedAtUtc",
                table: "localization_texts",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "localization_texts");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "users");
        }
    }
}
