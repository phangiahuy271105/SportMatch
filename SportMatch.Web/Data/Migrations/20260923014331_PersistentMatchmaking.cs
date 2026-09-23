using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportMatch.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersistentMatchmaking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HostName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Sport = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SportName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    District = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatchDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NeededPlayers = table.Column<int>(type: "int", nullable: false),
                    CostPerPerson = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchJoinRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatchPostId = table.Column<int>(type: "int", nullable: false),
                    ApplicantName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HoldExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchJoinRequests_MatchPosts_MatchPostId",
                        column: x => x.MatchPostId,
                        principalTable: "MatchPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchJoinRequests_MatchPostId",
                table: "MatchJoinRequests",
                column: "MatchPostId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchJoinRequests_PaymentTransactionId",
                table: "MatchJoinRequests",
                column: "PaymentTransactionId",
                unique: true,
                filter: "[PaymentTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MatchJoinRequests_RequestCode",
                table: "MatchJoinRequests",
                column: "RequestCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchPosts_MatchCode",
                table: "MatchPosts",
                column: "MatchCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchJoinRequests");

            migrationBuilder.DropTable(
                name: "MatchPosts");
        }
    }
}
