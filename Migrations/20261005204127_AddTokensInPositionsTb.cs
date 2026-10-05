using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kadroff.Migrations
{
    /// <inheritdoc />
    public partial class AddTokensInPositionsTb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PositionTokens_PositionId",
                table: "PositionTokens",
                column: "PositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionTokens_Positions_PositionId",
                table: "PositionTokens",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PositionTokens_Positions_PositionId",
                table: "PositionTokens");

            migrationBuilder.DropIndex(
                name: "IX_PositionTokens_PositionId",
                table: "PositionTokens");
        }
    }
}
