using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kadroff.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCvRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PdfByte",
                table: "Cvs",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_PositionId",
                table: "Cvs",
                column: "PositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cvs_Positions_PositionId",
                table: "Cvs",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cvs_Positions_PositionId",
                table: "Cvs");

            migrationBuilder.DropIndex(
                name: "IX_Cvs_PositionId",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "PdfByte",
                table: "Cvs");
        }
    }
}
