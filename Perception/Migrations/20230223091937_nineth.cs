using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Perception.Migrations
{
    /// <inheritdoc />
    public partial class nineth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Datasets_DatasetId",
                table: "Records");

            migrationBuilder.AlterColumn<int>(
                name: "DatasetId",
                table: "Records",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Datasets_DatasetId",
                table: "Records",
                column: "DatasetId",
                principalTable: "Datasets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Datasets_DatasetId",
                table: "Records");

            migrationBuilder.AlterColumn<int>(
                name: "DatasetId",
                table: "Records",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Datasets_DatasetId",
                table: "Records",
                column: "DatasetId",
                principalTable: "Datasets",
                principalColumn: "Id");
        }
    }
}
