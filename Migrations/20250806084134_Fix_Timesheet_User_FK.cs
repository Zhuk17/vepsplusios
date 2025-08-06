using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VepsPlusApi.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Timesheet_User_FK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_Users_UserId1",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_UserId1",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Timesheets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Timesheets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_UserId1",
                table: "Timesheets",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_Users_UserId1",
                table: "Timesheets",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
