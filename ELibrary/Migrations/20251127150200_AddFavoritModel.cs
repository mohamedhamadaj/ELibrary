using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoritModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Books_bookId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Promotions");

            migrationBuilder.RenameColumn(
                name: "bookId",
                table: "Promotions",
                newName: "BookId");

            migrationBuilder.RenameIndex(
                name: "IX_Promotions_bookId",
                table: "Promotions",
                newName: "IX_Promotions_BookId");

            migrationBuilder.AlterColumn<double>(
                name: "Discount",
                table: "Promotions",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "Carts",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => new { x.BookId, x.ApplicationUserId });
                    table.ForeignKey(
                        name: "FK_Favorites_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorites_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_ApplicationUserId",
                table: "Favorites",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Books_BookId",
                table: "Promotions",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Books_BookId",
                table: "Promotions");

            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.RenameColumn(
                name: "BookId",
                table: "Promotions",
                newName: "bookId");

            migrationBuilder.RenameIndex(
                name: "IX_Promotions_BookId",
                table: "Promotions",
                newName: "IX_Promotions_bookId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Discount",
                table: "Promotions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Promotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Carts",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Books_bookId",
                table: "Promotions",
                column: "bookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
