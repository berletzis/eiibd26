using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eiibd26.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNombrePaisToMedicoDirectorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFin",
                table: "tratamientoUsuario",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LatitudDefault",
                table: "Paises",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LongitudDefault",
                table: "Paises",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AreaExperienciaEii",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaExperienciaEii", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LaboratoryTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaModificado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaboratoryTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaboratoryTypes_LaboratoryTypes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "LaboratoryTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LaboratoryUnitCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Abreviatura = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaboratoryUnitCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicosDirectorio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreCompleto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CedulaProfesional = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Especialidad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Subespecialidad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ciudad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MunicipioAlcaldia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HospitalClinica = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NombrePais = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Latitud = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Longitud = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    EstatusValidacion = table.Column<int>(type: "int", nullable: false),
                    NivelConfianza = table.Column<int>(type: "int", nullable: false),
                    EstatusReclamacion = table.Column<int>(type: "int", nullable: false),
                    AspNetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaReclamacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VisiblePublicamente = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaModificacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PropuestoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicosDirectorio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicosDirectorio_AspNetUsers_AspNetUserId",
                        column: x => x.AspNetUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TipoConfirmacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoConfirmacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientLaboratoryResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaboratoryTypeId = table.Column<int>(type: "int", nullable: false),
                    ResultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResultUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResultDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LaboratoryUnitCatalogId = table.Column<int>(type: "int", nullable: true),
                    CondicionUsuarioId = table.Column<int>(type: "int", nullable: true),
                    SintomaUsuarioId = table.Column<int>(type: "int", nullable: true),
                    TratamientoUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaModificado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientLaboratoryResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientLaboratoryResults_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientLaboratoryResults_LaboratoryTypes_LaboratoryTypeId",
                        column: x => x.LaboratoryTypeId,
                        principalTable: "LaboratoryTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientLaboratoryResults_LaboratoryUnitCatalog_LaboratoryUnitCatalogId",
                        column: x => x.LaboratoryUnitCatalogId,
                        principalTable: "LaboratoryUnitCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientLaboratoryResults_condicionUsuario_CondicionUsuarioId",
                        column: x => x.CondicionUsuarioId,
                        principalTable: "condicionUsuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientLaboratoryResults_sintomasUsuario_SintomaUsuarioId",
                        column: x => x.SintomaUsuarioId,
                        principalTable: "sintomasUsuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientLaboratoryResults_tratamientoUsuario_TratamientoUsuarioId",
                        column: x => x.TratamientoUsuarioId,
                        principalTable: "tratamientoUsuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicoExperienciaEii",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicoDirectorioId = table.Column<int>(type: "int", nullable: false),
                    AreaExperienciaEiiId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicoExperienciaEii", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicoExperienciaEii_AreaExperienciaEii_AreaExperienciaEiiId",
                        column: x => x.AreaExperienciaEiiId,
                        principalTable: "AreaExperienciaEii",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicoExperienciaEii_MedicosDirectorio_MedicoDirectorioId",
                        column: x => x.MedicoDirectorioId,
                        principalTable: "MedicosDirectorio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfirmacionComunitaria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicoDirectorioId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoConfirmacionId = table.Column<int>(type: "int", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfirmacionComunitaria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfirmacionComunitaria_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfirmacionComunitaria_MedicosDirectorio_MedicoDirectorioId",
                        column: x => x.MedicoDirectorioId,
                        principalTable: "MedicosDirectorio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfirmacionComunitaria_TipoConfirmacion_TipoConfirmacionId",
                        column: x => x.TipoConfirmacionId,
                        principalTable: "TipoConfirmacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AreaExperienciaEii",
                columns: new[] { "Id", "Activo", "Descripcion", "Nombre", "Orden" },
                values: new object[,]
                {
                    { 1, true, null, "CUCI", 1 },
                    { 2, true, null, "Crohn", 2 },
                    { 3, true, null, "Pediátrico", 3 },
                    { 4, true, null, "Ostomías", 4 },
                    { 5, true, null, "Biológicos", 5 },
                    { 6, true, null, "Embarazo + EII", 6 },
                    { 7, true, null, "Manejo de brotes", 7 },
                    { 8, true, null, "Segunda opinión", 8 },
                    { 9, true, null, "Cirugía", 9 },
                    { 10, true, null, "Seguimiento prolongado", 10 }
                });

            migrationBuilder.InsertData(
                table: "TipoConfirmacion",
                columns: new[] { "Id", "Activo", "Descripcion", "Nombre", "Orden" },
                values: new object[,]
                {
                    { 1, true, null, "Me diagnosticó", 1 },
                    { 2, true, null, "Me ayudó con tratamiento biológico", 2 },
                    { 3, true, null, "Manejo de brotes", 3 },
                    { 4, true, null, "Segunda opinión", 4 },
                    { 5, true, null, "Cirugía", 5 },
                    { 6, true, null, "Seguimiento prolongado", 6 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmacionComunitaria_MedicoDirectorioId_UsuarioId_TipoConfirmacionId",
                table: "ConfirmacionComunitaria",
                columns: new[] { "MedicoDirectorioId", "UsuarioId", "TipoConfirmacionId" },
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmacionComunitaria_TipoConfirmacionId",
                table: "ConfirmacionComunitaria",
                column: "TipoConfirmacionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmacionComunitaria_UsuarioId",
                table: "ConfirmacionComunitaria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_LaboratoryTypes_ParentId",
                table: "LaboratoryTypes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicoExperienciaEii_AreaExperienciaEiiId",
                table: "MedicoExperienciaEii",
                column: "AreaExperienciaEiiId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicoExperienciaEii_MedicoDirectorioId_AreaExperienciaEiiId",
                table: "MedicoExperienciaEii",
                columns: new[] { "MedicoDirectorioId", "AreaExperienciaEiiId" },
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MedicosDirectorio_AspNetUserId",
                table: "MedicosDirectorio",
                column: "AspNetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicosDirectorio_CedulaProfesional",
                table: "MedicosDirectorio",
                column: "CedulaProfesional");

            migrationBuilder.CreateIndex(
                name: "IX_MedicosDirectorio_Estado_Ciudad",
                table: "MedicosDirectorio",
                columns: new[] { "Estado", "Ciudad" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientLaboratoryResults_CondicionUsuarioId",
                table: "PatientLaboratoryResults",
                column: "CondicionUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientLaboratoryResults_LaboratoryTypeId",
                table: "PatientLaboratoryResults",
                column: "LaboratoryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientLaboratoryResults_LaboratoryUnitCatalogId",
                table: "PatientLaboratoryResults",
                column: "LaboratoryUnitCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientLaboratoryResults_PatientId",
                table: "PatientLaboratoryResults",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientLaboratoryResults_SintomaUsuarioId",
                table: "PatientLaboratoryResults",
                column: "SintomaUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientLaboratoryResults_TratamientoUsuarioId",
                table: "PatientLaboratoryResults",
                column: "TratamientoUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfirmacionComunitaria");

            migrationBuilder.DropTable(
                name: "MedicoExperienciaEii");

            migrationBuilder.DropTable(
                name: "PatientLaboratoryResults");

            migrationBuilder.DropTable(
                name: "TipoConfirmacion");

            migrationBuilder.DropTable(
                name: "AreaExperienciaEii");

            migrationBuilder.DropTable(
                name: "MedicosDirectorio");

            migrationBuilder.DropTable(
                name: "LaboratoryTypes");

            migrationBuilder.DropTable(
                name: "LaboratoryUnitCatalog");

            migrationBuilder.DropColumn(
                name: "FechaFin",
                table: "tratamientoUsuario");

            migrationBuilder.DropColumn(
                name: "LatitudDefault",
                table: "Paises");

            migrationBuilder.DropColumn(
                name: "LongitudDefault",
                table: "Paises");
        }
    }
}
