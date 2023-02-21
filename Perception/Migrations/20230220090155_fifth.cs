using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Perception.Migrations
{
    /// <inheritdoc />
    public partial class fifth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GUID",
                table: "Records");

            migrationBuilder.AddColumn<int>(
                name: "RecordId",
                table: "Files",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_RecordId",
                table: "Files",
                column: "RecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Records_RecordId",
                table: "Files",
                column: "RecordId",
                principalTable: "Records",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Records_RecordId",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_RecordId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "RecordId",
                table: "Files");

            migrationBuilder.AddColumn<Guid>(
                name: "GUID",
                table: "Records",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
