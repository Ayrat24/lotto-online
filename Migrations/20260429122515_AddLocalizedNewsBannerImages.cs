using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizedNewsBannerImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePathEn",
                table: "news_banners",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePathRu",
                table: "news_banners",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePathUz",
                table: "news_banners",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePathEn",
                table: "news_banners");

            migrationBuilder.DropColumn(
                name: "ImagePathRu",
                table: "news_banners");

            migrationBuilder.DropColumn(
                name: "ImagePathUz",
                table: "news_banners");

        }
    }
}
