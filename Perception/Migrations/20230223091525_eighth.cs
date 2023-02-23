using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Perception.Migrations
{
    /// <inheritdoc />
    public partial class eighth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DatasetId",
                table: "Records",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Datasets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ready = table.Column<bool>(type: "bit", nullable: false),
                    Epoch = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datasets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Records_DatasetId",
                table: "Records",
                column: "DatasetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Datasets_DatasetId",
                table: "Records",
                column: "DatasetId",
                principalTable: "Datasets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Datasets_DatasetId",
                table: "Records");

            migrationBuilder.DropTable(
                name: "Datasets");

            migrationBuilder.DropIndex(
                name: "IX_Records_DatasetId",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "DatasetId",
                table: "Records");
        }
    }
}
