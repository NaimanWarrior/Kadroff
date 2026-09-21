using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kadroff.Migrations
{
    /// <inheritdoc />
    public partial class AddFields4TbJobApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AutoAcceptDate",
                table: "JobApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseComment",
                table: "JobApplications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoAcceptDate",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ResponseComment",
                table: "JobApplications");
        }
    }
}
