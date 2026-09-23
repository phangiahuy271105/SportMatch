using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportMatch.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CourtImagesAndDemoWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The demo database may have received part of this schema while the
            // feature was being developed. Guard every operation so restarting
            // the app can safely finish the migration instead of adding a column
            // that is already present.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.VenueComplexes', N'IsActive') IS NULL
                    ALTER TABLE [dbo].[VenueComplexes]
                    ADD [IsActive] bit NOT NULL
                        CONSTRAINT [DF_VenueComplexes_IsActive] DEFAULT CAST(1 AS bit);

                IF COL_LENGTH(N'dbo.SportCourts', N'ImagePath') IS NULL
                    ALTER TABLE [dbo].[SportCourts]
                    ADD [ImagePath] nvarchar(300) NULL;

                IF COL_LENGTH(N'dbo.SportCourts', N'IsActive') IS NULL
                    ALTER TABLE [dbo].[SportCourts]
                    ADD [IsActive] bit NOT NULL
                        CONSTRAINT [DF_SportCourts_IsActive] DEFAULT CAST(1 AS bit);

                IF COL_LENGTH(N'dbo.MatchPosts', N'SourceBookingId') IS NULL
                    ALTER TABLE [dbo].[MatchPosts]
                    ADD [SourceBookingId] int NULL;

                IF COL_LENGTH(N'dbo.Bookings', N'MatchCostPerPerson') IS NULL
                    ALTER TABLE [dbo].[Bookings]
                    ADD [MatchCostPerPerson] decimal(18,0) NOT NULL
                        CONSTRAINT [DF_Bookings_MatchCostPerPerson] DEFAULT CAST(0 AS decimal(18,0));

                IF COL_LENGTH(N'dbo.Bookings', N'MatchLevel') IS NULL
                    ALTER TABLE [dbo].[Bookings]
                    ADD [MatchLevel] nvarchar(30) NOT NULL
                        CONSTRAINT [DF_Bookings_MatchLevel] DEFAULT N'Intermediate';

                IF COL_LENGTH(N'dbo.Bookings', N'MatchNeededPlayers') IS NULL
                    ALTER TABLE [dbo].[Bookings]
                    ADD [MatchNeededPlayers] int NOT NULL
                        CONSTRAINT [DF_Bookings_MatchNeededPlayers] DEFAULT 2;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_MatchPosts_SourceBookingId'
                      AND [object_id] = OBJECT_ID(N'dbo.MatchPosts')
                )
                    CREATE UNIQUE INDEX [IX_MatchPosts_SourceBookingId]
                    ON [dbo].[MatchPosts] ([SourceBookingId])
                    WHERE [SourceBookingId] IS NOT NULL;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [name] = N'FK_MatchPosts_Bookings_SourceBookingId'
                      AND [parent_object_id] = OBJECT_ID(N'dbo.MatchPosts')
                )
                    ALTER TABLE [dbo].[MatchPosts] WITH CHECK
                    ADD CONSTRAINT [FK_MatchPosts_Bookings_SourceBookingId]
                    FOREIGN KEY ([SourceBookingId]) REFERENCES [dbo].[Bookings] ([Id])
                    ON DELETE SET NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchPosts_Bookings_SourceBookingId",
                table: "MatchPosts");

            migrationBuilder.DropIndex(
                name: "IX_MatchPosts_SourceBookingId",
                table: "MatchPosts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "VenueComplexes");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "SportCourts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SportCourts");

            migrationBuilder.DropColumn(
                name: "SourceBookingId",
                table: "MatchPosts");

            migrationBuilder.DropColumn(
                name: "MatchCostPerPerson",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "MatchLevel",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "MatchNeededPlayers",
                table: "Bookings");
        }
    }
}
