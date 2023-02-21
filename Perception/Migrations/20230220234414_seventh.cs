using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Perception.Migrations
{
    /// <inheritdoc />
    public partial class seventh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Results_Records_RecordId",
                table: "Results");

            migrationBuilder.RenameColumn(
                name: "RecordId",
                table: "Results",
                newName: "FileId");

            migrationBuilder.RenameIndex(
                name: "IX_Results_RecordId",
                table: "Results",
                newName: "IX_Results_FileId");

            migrationBuilder.AlterColumn<string>(
                name: "Class",
                table: "Results",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Results_Files_FileId",
                table: "Results",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Results_Files_FileId",
                table: "Results");

            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "Results",
                newName: "RecordId");

            migrationBuilder.RenameIndex(
                name: "IX_Results_FileId",
                table: "Results",
                newName: "IX_Results_RecordId");

            migrationBuilder.AlterColumn<int>(
                name: "Class",
                table: "Results",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Results_Records_RecordId",
                table: "Results",
                column: "RecordId",
                principalTable: "Records",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
