using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordBot.Migrations
{
    public partial class Milestones : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastMilestoneDays",
                table: "Sobrieties",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<ulong>(
                name: "MilestoneChannelID",
                table: "Config",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    ID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Days = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.ID);
                });

            migrationBuilder.InsertData(
                "Milestones",
                new string[] { "Days", "Name" },
                new object[,]
                {
                    { 7, "1 Week" },
                    { 30, "1 Month" },
                    { 60, "2 Months" },
                    { 90, "3 Months" },
                    { 180, "6 Months" },
                    { 270, "9 Months" },
                    { 365, "1 Year" },
                    { 545, "18 Months" },
                    { 365 * 2, "2 Years" },
                    { 365 * 3, "3 Years" },
                    { 365 * 4, "4 Years" },
                    { 365 * 5, "5 Years" },
                    { 365 * 6, "6 Years" },
                    { 365 * 7, "7 Years" },
                    { 365 * 8, "8 Years" },
                    { 365 * 9, "9 Years" },
                    { 365 * 10, "10 Years" },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropColumn(
                name: "LastMilestoneDays",
                table: "Sobrieties");

            migrationBuilder.DropColumn(
                name: "MilestoneChannelID",
                table: "Config");
        }
    }
}
