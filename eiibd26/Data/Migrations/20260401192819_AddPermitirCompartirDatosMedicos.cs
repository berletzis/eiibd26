using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eiibd26.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPermitirCompartirDatosMedicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PermitirCompartirDatosMedicos",
                table: "Perfil",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GlossaryTermRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TermId = table.Column<int>(type: "int", nullable: false),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryTermRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlossaryTermRatings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GlossaryTermRatings_GlossaryTerm_TermId",
                        column: x => x.TermId,
                        principalTable: "GlossaryTerm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTermRatings_TermId",
                table: "GlossaryTermRatings",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTermRatings_UserId",
                table: "GlossaryTermRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTermRatings_UserId_TermId",
                table: "GlossaryTermRatings",
                columns: new[] { "UserId", "TermId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlossaryTermRatings");

            migrationBuilder.DropColumn(
                name: "PermitirCompartirDatosMedicos",
                table: "Perfil");
        }
    }
}
