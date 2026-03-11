using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eiibd26.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGlossaryValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AspNetUserTokens",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPasswordReset",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "AspNetUserRoles",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AspNetUserRoles",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AspNetUserLogins",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AspNetUserClaims",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "AspNetRoles",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "AspNetRoleClaims",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "Aplicaciones",
                columns: table => new
                {
                    idApp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idPerfil = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    fecha_actualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aplicaciones", x => x.idApp);
                });

            migrationBuilder.CreateTable(
                name: "BannersInicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsImage = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    VisibleToAuthenticated = table.Column<bool>(type: "bit", nullable: false),
                    VisibleToAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AgregadoPor = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AgregadoPor_Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImagePathDesktop = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageFileNameDesktop = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ImageContentTypeDesktop = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageSizeDesktop = table.Column<long>(type: "bigint", nullable: true),
                    ImagePathMobile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageFileNameMobile = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ImageContentTypeMobile = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageSizeMobile = table.Column<long>(type: "bigint", nullable: true),
                    TargetUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Borrado = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannersInicio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "condiciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    idPadre = table.Column<int>(type: "int", nullable: true),
                    idIdioma = table.Column<int>(type: "int", nullable: true),
                    icono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    fechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    DefaultRegistro = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_condiciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_condiciones_condiciones_idPadre",
                        column: x => x.idPadre,
                        principalTable: "condiciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contenidosCategorias",
                columns: table => new
                {
                    Sequence = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoriaPadre = table.Column<int>(type: "int", nullable: true),
                    Clave = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Imagen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Relevante = table.Column<bool>(type: "bit", nullable: true),
                    CategoriaSlug = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Borrado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidosCategorias", x => x.Sequence);
                    table.ForeignKey(
                        name: "FK_contenidosCategorias_contenidosCategorias_CategoriaPadre",
                        column: x => x.CategoriaPadre,
                        principalTable: "contenidosCategorias",
                        principalColumn: "Sequence",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "estudiosLab",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    idPadre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    idIdioma = table.Column<int>(type: "int", nullable: false),
                    icono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    fechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estudiosLab", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Etiquetas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NombreCanonico = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fuente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sinonimos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etiquetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlossaryTerm",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoTermino = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MedicalRelationSuggestedId = table.Column<int>(type: "int", nullable: true),
                    MedicalRelationTypeId = table.Column<int>(type: "int", nullable: true),
                    AiReasoning = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByAI = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryTerm", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    P256dh = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Auth = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUsed = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Paises",
                columns: table => new
                {
                    PaisCodigo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    PaisNombre = table.Column<string>(type: "nvarchar(52)", maxLength: 52, nullable: false),
                    PaisContinente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaisRegion = table.Column<string>(type: "nvarchar(26)", maxLength: 26, nullable: false),
                    PaisNombreLocal = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    PaisCapital = table.Column<int>(type: "int", nullable: true),
                    VIsibleBuscador = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Borrado = table.Column<bool>(type: "bit", nullable: false),
                    PaisLat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PaisLong = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paises", x => x.PaisCodigo);
                });

            migrationBuilder.CreateTable(
                name: "Preguntas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Cuerpo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Resuelta = table.Column<bool>(type: "bit", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaModificacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TieneRespuestaIA = table.Column<bool>(type: "bit", nullable: false),
                    FechaGeneracionIA = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preguntas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ScheduledFor = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsSent = table.Column<bool>(type: "bit", nullable: false),
                    TargetUserIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalSent = table.Column<int>(type: "int", nullable: false),
                    TotalFailed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushNotifications_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "sintomas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    idPadre = table.Column<int>(type: "int", nullable: true),
                    idIdioma = table.Column<int>(type: "int", nullable: true),
                    icono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DescripcionIA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidadoIA = table.Column<bool>(type: "bit", nullable: false),
                    ValidadoHumano = table.Column<bool>(type: "bit", nullable: false),
                    RelacionEIIDescripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RelacionEII = table.Column<bool>(type: "bit", nullable: false),
                    Fuentes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaActualizacionIA = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sintomas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tratamientos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    idPadre = table.Column<int>(type: "int", nullable: true),
                    idIdioma = table.Column<int>(type: "int", nullable: true),
                    icono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DescripcionIA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidadoIA = table.Column<bool>(type: "bit", nullable: false),
                    ValidadoHumano = table.Column<bool>(type: "bit", nullable: false),
                    RelacionEIIDescripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RelacionEII = table.Column<bool>(type: "bit", nullable: false),
                    Fuentes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaActualizacionIA = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tratamientos", x => x.id);
                    table.ForeignKey(
                        name: "FK_tratamientos_tratamientos_idPadre",
                        column: x => x.idPadre,
                        principalTable: "tratamientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Votos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntidadTipo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<short>(type: "smallint", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaModificacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votos_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "condicionUsuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idCondicion = table.Column<int>(type: "int", nullable: true),
                    idUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fechaInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_condicionUsuario", x => x.id);
                    table.ForeignKey(
                        name: "FK_condicionUsuario_AspNetUsers_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_condicionUsuario_condiciones_idCondicion",
                        column: x => x.idCondicion,
                        principalTable: "condiciones",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "estudiosLabUsuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idestudiosLab = table.Column<int>(type: "int", nullable: false),
                    idUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estudiosLabUsuario", x => x.id);
                    table.ForeignKey(
                        name: "FK_estudiosLabUsuario_AspNetUsers_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_estudiosLabUsuario_estudiosLab_idestudiosLab",
                        column: x => x.idestudiosLab,
                        principalTable: "estudiosLab",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GlossaryTermMedicalLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GlossaryTermId = table.Column<int>(type: "int", nullable: false),
                    SintomaId = table.Column<int>(type: "int", nullable: true),
                    TratamientoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryTermMedicalLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlossaryTermMedicalLink_GlossaryTerm_GlossaryTermId",
                        column: x => x.GlossaryTermId,
                        principalTable: "GlossaryTerm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GlossaryValidation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GlossaryTermId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ValidationType = table.Column<int>(type: "int", nullable: false),
                    MedicalRelationTypeId = table.Column<int>(type: "int", nullable: true),
                    Approved = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryValidation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlossaryValidation_GlossaryTerm_GlossaryTermId",
                        column: x => x.GlossaryTermId,
                        principalTable: "GlossaryTerm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ZonaHoraria",
                columns: table => new
                {
                    idZone = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    zoneinfo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    offset = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    summer = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PaisCodigo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    cicode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    cicodesummer = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    offset_seconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZonaHoraria", x => x.idZone);
                    table.ForeignKey(
                        name: "FK_ZonaHoraria_Paises_PaisCodigo",
                        column: x => x.PaisCodigo,
                        principalTable: "Paises",
                        principalColumn: "PaisCodigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreguntaCondiciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreguntaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CondicionId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreguntaCondiciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreguntaCondiciones_Preguntas_PreguntaId",
                        column: x => x.PreguntaId,
                        principalTable: "Preguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreguntaCondiciones_condiciones_CondicionId",
                        column: x => x.CondicionId,
                        principalTable: "condiciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreguntaEtiquetas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreguntaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EtiquetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaRelacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreguntaEtiquetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreguntaEtiquetas_Preguntas_PreguntaId",
                        column: x => x.PreguntaId,
                        principalTable: "Preguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Respuestas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreguntaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cuerpo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EsAceptada = table.Column<bool>(type: "bit", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaModificacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ParentRespuestaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EsIA = table.Column<bool>(type: "bit", nullable: false),
                    ModeloIA = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EsColapsada = table.Column<bool>(type: "bit", nullable: false),
                    Puntuacion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Respuestas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Respuestas_Preguntas_PreguntaId",
                        column: x => x.PreguntaId,
                        principalTable: "Preguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Respuestas_Respuestas_ParentRespuestaId",
                        column: x => x.ParentRespuestaId,
                        principalTable: "Respuestas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PreguntaSintomas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreguntaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SintomaId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreguntaSintomas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreguntaSintomas_Preguntas_PreguntaId",
                        column: x => x.PreguntaId,
                        principalTable: "Preguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreguntaSintomas_sintomas_SintomaId",
                        column: x => x.SintomaId,
                        principalTable: "sintomas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SintomasNotas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SintomaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EsNotaIA = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SintomasNotas", x => x.id);
                    table.ForeignKey(
                        name: "FK_SintomasNotas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SintomasNotas_sintomas_SintomaId",
                        column: x => x.SintomaId,
                        principalTable: "sintomas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sintomasUsuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSintoma = table.Column<int>(type: "int", nullable: true),
                    idUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sintomasUsuario", x => x.id);
                    table.ForeignKey(
                        name: "FK_sintomasUsuario_AspNetUsers_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sintomasUsuario_sintomas_idSintoma",
                        column: x => x.idSintoma,
                        principalTable: "sintomas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "PreguntaTratamientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreguntaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TratamientoId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreguntaTratamientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreguntaTratamientos_Preguntas_PreguntaId",
                        column: x => x.PreguntaId,
                        principalTable: "Preguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreguntaTratamientos_tratamientos_TratamientoId",
                        column: x => x.TratamientoId,
                        principalTable: "tratamientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TratamientosNotas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TratamientoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EsNotaIA = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TratamientosNotas", x => x.id);
                    table.ForeignKey(
                        name: "FK_TratamientosNotas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TratamientosNotas_tratamientos_TratamientoId",
                        column: x => x.TratamientoId,
                        principalTable: "tratamientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tratamientoUsuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCondicion = table.Column<int>(type: "int", nullable: true),
                    idTratamiento = table.Column<int>(type: "int", nullable: true),
                    idUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tratamientoUsuario", x => x.id);
                    table.ForeignKey(
                        name: "FK_tratamientoUsuario_AspNetUsers_idUsuario",
                        column: x => x.idUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tratamientoUsuario_condiciones_IdCondicion",
                        column: x => x.IdCondicion,
                        principalTable: "condiciones",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tratamientoUsuario_tratamientos_idTratamiento",
                        column: x => x.idTratamiento,
                        principalTable: "tratamientos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Perfil",
                columns: table => new
                {
                    idUser = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    imagenFondo = table.Column<int>(type: "int", nullable: true),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaDeNacimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstoyAqui = table.Column<int>(type: "int", nullable: true),
                    idZone = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimaActividad = table.Column<DateTime>(type: "datetime2", nullable: true),
                    slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Genero = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Latitud = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Longitud = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NombreCiudad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NombrePais = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AceptoPP = table.Column<bool>(type: "bit", nullable: true),
                    AcercaDe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaModificado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PermitirTelefonoReal = table.Column<bool>(type: "bit", nullable: true),
                    PermitirCorreoNoticias = table.Column<bool>(type: "bit", nullable: true),
                    PermitirMostrarPais = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfil", x => x.idUser);
                    table.ForeignKey(
                        name: "FK_Perfil_AspNetUsers_idUser",
                        column: x => x.idUser,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Perfil_ZonaHoraria_idZone",
                        column: x => x.idZone,
                        principalTable: "ZonaHoraria",
                        principalColumn: "idZone",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RespuestaAIFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RespuestaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EsUtil = table.Column<bool>(type: "bit", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestaAIFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespuestaAIFeedback_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RespuestaAIFeedback_Respuestas_RespuestaId",
                        column: x => x.RespuestaId,
                        principalTable: "Respuestas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SintomaCondicionUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdCondicionUsuario = table.Column<int>(type: "int", nullable: false),
                    IdSintomaUsuario = table.Column<int>(type: "int", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SintomaCondicionUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SintomaCondicionUsuario_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SintomaCondicionUsuario_condicionUsuario_IdCondicionUsuario",
                        column: x => x.IdCondicionUsuario,
                        principalTable: "condicionUsuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SintomaCondicionUsuario_sintomasUsuario_IdSintomaUsuario",
                        column: x => x.IdSintomaUsuario,
                        principalTable: "sintomasUsuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackingSintomaUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdSintomaUsuario = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingSintomaUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingSintomaUsuario_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrackingSintomaUsuario_sintomasUsuario_IdSintomaUsuario",
                        column: x => x.IdSintomaUsuario,
                        principalTable: "sintomasUsuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstadoAnimoUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstadoMood = table.Column<int>(type: "int", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    IdCondicionUsuario = table.Column<int>(type: "int", nullable: true),
                    IdSintomaUsuario = table.Column<int>(type: "int", nullable: true),
                    IdTratamientoUsuario = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoAnimoUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstadoAnimoUsuario_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstadoAnimoUsuario_condicionUsuario_IdCondicionUsuario",
                        column: x => x.IdCondicionUsuario,
                        principalTable: "condicionUsuario",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_EstadoAnimoUsuario_sintomasUsuario_IdSintomaUsuario",
                        column: x => x.IdSintomaUsuario,
                        principalTable: "sintomasUsuario",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_EstadoAnimoUsuario_tratamientoUsuario_IdTratamientoUsuario",
                        column: x => x.IdTratamientoUsuario,
                        principalTable: "tratamientoUsuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "TratamientoCondicionUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdCondicionUsuario = table.Column<int>(type: "int", nullable: true),
                    IdTratamientoUsuario = table.Column<int>(type: "int", nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TratamientoCondicionUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TratamientoCondicionUsuario_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TratamientoCondicionUsuario_condicionUsuario_IdCondicionUsuario",
                        column: x => x.IdCondicionUsuario,
                        principalTable: "condicionUsuario",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_TratamientoCondicionUsuario_tratamientoUsuario_IdTratamientoUsuario",
                        column: x => x.IdTratamientoUsuario,
                        principalTable: "tratamientoUsuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "TratamientoSintomaUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdSintomaUsuario = table.Column<int>(type: "int", nullable: true),
                    IdTratamientoUsuario = table.Column<int>(type: "int", nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TratamientoSintomaUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TratamientoSintomaUsuario_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TratamientoSintomaUsuario_sintomasUsuario_IdSintomaUsuario",
                        column: x => x.IdSintomaUsuario,
                        principalTable: "sintomasUsuario",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_TratamientoSintomaUsuario_tratamientoUsuario_IdTratamientoUsuario",
                        column: x => x.IdTratamientoUsuario,
                        principalTable: "tratamientoUsuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "contenidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTipo = table.Column<int>(type: "int", nullable: true),
                    ContenidoTitulo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContenidoTextoC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContenidoTextoL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContenidoTituloSlug = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    URLImagenPrincipal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstadoPublicacion = table.Column<int>(type: "int", nullable: true),
                    ContenidoFechaInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContenidoFechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdAutor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Autor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdEmpresa = table.Column<int>(type: "int", nullable: true),
                    PaisClave = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaModificado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    IdUser = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contenidos_AspNetUsers_IdUser",
                        column: x => x.IdUser,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_contenidos_Perfil_IdAutor",
                        column: x => x.IdAutor,
                        principalTable: "Perfil",
                        principalColumn: "idUser",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleRatings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ArticleRatings_contenidos_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contenidoCondicionesRelacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContenidoId = table.Column<int>(type: "int", nullable: false),
                    CondicionId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Borrado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidoCondicionesRelacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contenidoCondicionesRelacion_condiciones_CondicionId",
                        column: x => x.CondicionId,
                        principalTable: "condiciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contenidoCondicionesRelacion_contenidos_ContenidoId",
                        column: x => x.ContenidoId,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contenidosCalificacion_ArticulosPreguntas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idContenido = table.Column<int>(type: "int", nullable: false),
                    digito = table.Column<int>(type: "int", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    fechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidosCalificacion_ArticulosPreguntas", x => x.id);
                    table.ForeignKey(
                        name: "FK_contenidosCalificacion_ArticulosPreguntas_contenidos_idContenido",
                        column: x => x.idContenido,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contenidosCategoriasRelacion",
                columns: table => new
                {
                    Sequence = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdContenido = table.Column<int>(type: "int", nullable: false),
                    IdPerfil = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdEmpresa = table.Column<int>(type: "int", nullable: true),
                    IdCategoria = table.Column<int>(type: "int", nullable: true),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Borrado = table.Column<bool>(type: "bit", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidosCategoriasRelacion", x => x.Sequence);
                    table.ForeignKey(
                        name: "FK_contenidosCategoriasRelacion_contenidosCategorias_IdCategoria",
                        column: x => x.IdCategoria,
                        principalTable: "contenidosCategorias",
                        principalColumn: "Sequence");
                    table.ForeignKey(
                        name: "FK_contenidosCategoriasRelacion_contenidos_IdContenido",
                        column: x => x.IdContenido,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contenidoSintomasRelacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContenidoId = table.Column<int>(type: "int", nullable: false),
                    SintomaId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Borrado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidoSintomasRelacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contenidoSintomasRelacion_contenidos_ContenidoId",
                        column: x => x.ContenidoId,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contenidoSintomasRelacion_sintomas_SintomaId",
                        column: x => x.SintomaId,
                        principalTable: "sintomas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contenidosPreguntasRelacion",
                columns: table => new
                {
                    Sequence = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idContenido = table.Column<int>(type: "int", nullable: false),
                    PreguntaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Borrado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidosPreguntasRelacion", x => x.Sequence);
                    table.ForeignKey(
                        name: "FK_contenidosPreguntasRelacion_contenidos_idContenido",
                        column: x => x.idContenido,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contenidosRelacionados",
                columns: table => new
                {
                    Sequence = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdContenido = table.Column<int>(type: "int", nullable: false),
                    IdContenidoRelacionado = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Borrado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidosRelacionados", x => x.Sequence);
                    table.ForeignKey(
                        name: "FK_contenidosRelacionados_contenidos_IdContenido",
                        column: x => x.IdContenido,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contenidosRelacionados_contenidos_IdContenidoRelacionado",
                        column: x => x.IdContenidoRelacionado,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contenidosRespuestas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContenidoId = table.Column<int>(type: "int", nullable: false),
                    Metas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RespuestaTitulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RespuestaTextoC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RespuestaTextoL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    URLImagenPrincipal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EsPopular = table.Column<bool>(type: "bit", nullable: true),
                    FechaPublicacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    IdAutor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Autor = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidosRespuestas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contenidosRespuestas_Perfil_IdAutor",
                        column: x => x.IdAutor,
                        principalTable: "Perfil",
                        principalColumn: "idUser",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contenidosRespuestas_contenidos_ContenidoId",
                        column: x => x.ContenidoId,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contenidosRespuestasRelacion",
                columns: table => new
                {
                    Sequence = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idContenido = table.Column<int>(type: "int", nullable: false),
                    RespuestaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Borrado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidosRespuestasRelacion", x => x.Sequence);
                    table.ForeignKey(
                        name: "FK_contenidosRespuestasRelacion_contenidos_idContenido",
                        column: x => x.idContenido,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contenidoTratamientosRelacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContenidoId = table.Column<int>(type: "int", nullable: false),
                    TratamientoId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioModificacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Borrado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidoTratamientosRelacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contenidoTratamientosRelacion_contenidos_ContenidoId",
                        column: x => x.ContenidoId,
                        principalTable: "contenidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contenidoTratamientosRelacion_tratamientos_TratamientoId",
                        column: x => x.TratamientoId,
                        principalTable: "tratamientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contenidosCalificacion_Respuestas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idContenidoRespuesta = table.Column<int>(type: "int", nullable: false),
                    digito = table.Column<int>(type: "int", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    fechaCreado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaModificado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fechaEliminado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenidosCalificacion_Respuestas", x => x.id);
                    table.ForeignKey(
                        name: "FK_contenidosCalificacion_Respuestas_contenidosRespuestas_idContenidoRespuesta",
                        column: x => x.idContenidoRespuesta,
                        principalTable: "contenidosRespuestas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleRatings_ArticleId",
                table: "ArticleRatings",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleRatings_UserId",
                table: "ArticleRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleRatings_UserId_ArticleId",
                table: "ArticleRatings",
                columns: new[] { "UserId", "ArticleId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_condiciones_idPadre",
                table: "condiciones",
                column: "idPadre");

            migrationBuilder.CreateIndex(
                name: "IX_condicionUsuario_idCondicion",
                table: "condicionUsuario",
                column: "idCondicion");

            migrationBuilder.CreateIndex(
                name: "IX_condicionUsuario_idUsuario",
                table: "condicionUsuario",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_contenidoCondicionesRelacion_CondicionId",
                table: "contenidoCondicionesRelacion",
                column: "CondicionId");

            migrationBuilder.CreateIndex(
                name: "IX_contenidoCondicionesRelacion_ContenidoId_CondicionId",
                table: "contenidoCondicionesRelacion",
                columns: new[] { "ContenidoId", "CondicionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contenidos_ContenidoFechaFin",
                table: "contenidos",
                column: "ContenidoFechaFin");

            migrationBuilder.CreateIndex(
                name: "IX_contenidos_ContenidoFechaInicio",
                table: "contenidos",
                column: "ContenidoFechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_contenidos_IdAutor",
                table: "contenidos",
                column: "IdAutor");

            migrationBuilder.CreateIndex(
                name: "IX_contenidos_IdUser",
                table: "contenidos",
                column: "IdUser");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosCalificacion_ArticulosPreguntas_idContenido",
                table: "contenidosCalificacion_ArticulosPreguntas",
                column: "idContenido");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosCalificacion_Respuestas_idContenidoRespuesta",
                table: "contenidosCalificacion_Respuestas",
                column: "idContenidoRespuesta");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosCategorias_CategoriaPadre",
                table: "contenidosCategorias",
                column: "CategoriaPadre");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosCategorias_Nombre",
                table: "contenidosCategorias",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosCategoriasRelacion_IdCategoria",
                table: "contenidosCategoriasRelacion",
                column: "IdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosCategoriasRelacion_IdContenido",
                table: "contenidosCategoriasRelacion",
                column: "IdContenido");

            migrationBuilder.CreateIndex(
                name: "IX_contenidoSintomasRelacion_ContenidoId_SintomaId",
                table: "contenidoSintomasRelacion",
                columns: new[] { "ContenidoId", "SintomaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contenidoSintomasRelacion_SintomaId",
                table: "contenidoSintomasRelacion",
                column: "SintomaId");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosPreguntasRelacion_idContenido",
                table: "contenidosPreguntasRelacion",
                column: "idContenido");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosPreguntasRelacion_idContenido_PreguntaId",
                table: "contenidosPreguntasRelacion",
                columns: new[] { "idContenido", "PreguntaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contenidosPreguntasRelacion_PreguntaId",
                table: "contenidosPreguntasRelacion",
                column: "PreguntaId");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosRelacionados_IdContenido",
                table: "contenidosRelacionados",
                column: "IdContenido");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosRelacionados_IdContenidoRelacionado",
                table: "contenidosRelacionados",
                column: "IdContenidoRelacionado");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosRespuestas_ContenidoId",
                table: "contenidosRespuestas",
                column: "ContenidoId");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosRespuestas_IdAutor",
                table: "contenidosRespuestas",
                column: "IdAutor");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosRespuestasRelacion_idContenido",
                table: "contenidosRespuestasRelacion",
                column: "idContenido");

            migrationBuilder.CreateIndex(
                name: "IX_contenidosRespuestasRelacion_idContenido_RespuestaId",
                table: "contenidosRespuestasRelacion",
                columns: new[] { "idContenido", "RespuestaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contenidosRespuestasRelacion_RespuestaId",
                table: "contenidosRespuestasRelacion",
                column: "RespuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_contenidoTratamientosRelacion_ContenidoId_TratamientoId",
                table: "contenidoTratamientosRelacion",
                columns: new[] { "ContenidoId", "TratamientoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contenidoTratamientosRelacion_TratamientoId",
                table: "contenidoTratamientosRelacion",
                column: "TratamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoAnimoUsuario_IdCondicionUsuario",
                table: "EstadoAnimoUsuario",
                column: "IdCondicionUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoAnimoUsuario_IdSintomaUsuario",
                table: "EstadoAnimoUsuario",
                column: "IdSintomaUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoAnimoUsuario_IdTratamientoUsuario",
                table: "EstadoAnimoUsuario",
                column: "IdTratamientoUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoAnimoUsuario_IdUsuario",
                table: "EstadoAnimoUsuario",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_estudiosLabUsuario_idestudiosLab",
                table: "estudiosLabUsuario",
                column: "idestudiosLab");

            migrationBuilder.CreateIndex(
                name: "IX_estudiosLabUsuario_idUsuario",
                table: "estudiosLabUsuario",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Etiquetas_NombreCanonico_Tipo",
                table: "Etiquetas",
                columns: new[] { "NombreCanonico", "Tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTermMedicalLink_GlossaryTermId",
                table: "GlossaryTermMedicalLink",
                column: "GlossaryTermId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryValidation_GlossaryTermId",
                table: "GlossaryValidation",
                column: "GlossaryTermId");

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryValidation_GlossaryTermId_UserId_ValidationType",
                table: "GlossaryValidation",
                columns: new[] { "GlossaryTermId", "UserId", "ValidationType" },
                unique: true,
                filter: "[MedicalRelationTypeId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryValidation_GlossaryTermId_UserId_ValidationType_MedicalRelationTypeId",
                table: "GlossaryValidation",
                columns: new[] { "GlossaryTermId", "UserId", "ValidationType", "MedicalRelationTypeId" },
                unique: true,
                filter: "[MedicalRelationTypeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryValidation_UserId",
                table: "GlossaryValidation",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSubscriptions_UserId",
                table: "NotificationSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_idZone",
                table: "Perfil",
                column: "idZone");

            migrationBuilder.CreateIndex(
                name: "IX_PreguntaCondiciones_CondicionId",
                table: "PreguntaCondiciones",
                column: "CondicionId");

            migrationBuilder.CreateIndex(
                name: "IX_PreguntaCondiciones_PreguntaId_CondicionId",
                table: "PreguntaCondiciones",
                columns: new[] { "PreguntaId", "CondicionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreguntaEtiquetas_PreguntaId_EtiquetaId",
                table: "PreguntaEtiquetas",
                columns: new[] { "PreguntaId", "EtiquetaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Preguntas_FechaCreacion",
                table: "Preguntas",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_Preguntas_UsuarioId",
                table: "Preguntas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PreguntaSintomas_PreguntaId_SintomaId",
                table: "PreguntaSintomas",
                columns: new[] { "PreguntaId", "SintomaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreguntaSintomas_SintomaId",
                table: "PreguntaSintomas",
                column: "SintomaId");

            migrationBuilder.CreateIndex(
                name: "IX_PreguntaTratamientos_PreguntaId_TratamientoId",
                table: "PreguntaTratamientos",
                columns: new[] { "PreguntaId", "TratamientoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreguntaTratamientos_TratamientoId",
                table: "PreguntaTratamientos",
                column: "TratamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_PushNotifications_CreatedBy",
                table: "PushNotifications",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaAIFeedback_RespuestaId",
                table: "RespuestaAIFeedback",
                column: "RespuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaAIFeedback_UsuarioId",
                table: "RespuestaAIFeedback",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Respuestas_ParentRespuestaId",
                table: "Respuestas",
                column: "ParentRespuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_Respuestas_PreguntaId",
                table: "Respuestas",
                column: "PreguntaId");

            migrationBuilder.CreateIndex(
                name: "IX_Respuestas_UsuarioId",
                table: "Respuestas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SintomaCondicionUsuario_IdCondicionUsuario",
                table: "SintomaCondicionUsuario",
                column: "IdCondicionUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_SintomaCondicionUsuario_IdSintomaUsuario",
                table: "SintomaCondicionUsuario",
                column: "IdSintomaUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_SintomaCondicionUsuario_IdUsuario",
                table: "SintomaCondicionUsuario",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_SintomasNotas_SintomaId",
                table: "SintomasNotas",
                column: "SintomaId");

            migrationBuilder.CreateIndex(
                name: "IX_SintomasNotas_UsuarioId",
                table: "SintomasNotas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_sintomasUsuario_idSintoma",
                table: "sintomasUsuario",
                column: "idSintoma");

            migrationBuilder.CreateIndex(
                name: "IX_sintomasUsuario_idUsuario",
                table: "sintomasUsuario",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSintomaUsuario_IdSintomaUsuario",
                table: "TrackingSintomaUsuario",
                column: "IdSintomaUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSintomaUsuario_IdUsuario",
                table: "TrackingSintomaUsuario",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_TratamientoCondicionUsuario_IdCondicionUsuario",
                table: "TratamientoCondicionUsuario",
                column: "IdCondicionUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_TratamientoCondicionUsuario_IdTratamientoUsuario",
                table: "TratamientoCondicionUsuario",
                column: "IdTratamientoUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_TratamientoCondicionUsuario_IdUsuario",
                table: "TratamientoCondicionUsuario",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_tratamientos_idPadre",
                table: "tratamientos",
                column: "idPadre");

            migrationBuilder.CreateIndex(
                name: "IX_TratamientoSintomaUsuario_IdSintomaUsuario",
                table: "TratamientoSintomaUsuario",
                column: "IdSintomaUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_TratamientoSintomaUsuario_IdTratamientoUsuario",
                table: "TratamientoSintomaUsuario",
                column: "IdTratamientoUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_TratamientoSintomaUsuario_IdUsuario",
                table: "TratamientoSintomaUsuario",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_TratamientosNotas_TratamientoId",
                table: "TratamientosNotas",
                column: "TratamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_TratamientosNotas_UsuarioId",
                table: "TratamientosNotas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_tratamientoUsuario_IdCondicion",
                table: "tratamientoUsuario",
                column: "IdCondicion");

            migrationBuilder.CreateIndex(
                name: "IX_tratamientoUsuario_idTratamiento",
                table: "tratamientoUsuario",
                column: "idTratamiento");

            migrationBuilder.CreateIndex(
                name: "IX_tratamientoUsuario_idUsuario",
                table: "tratamientoUsuario",
                column: "idUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Votos_EntidadTipo_EntidadId",
                table: "Votos",
                columns: new[] { "EntidadTipo", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_Votos_EntidadTipo_EntidadId_UsuarioId",
                table: "Votos",
                columns: new[] { "EntidadTipo", "EntidadId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votos_UsuarioId",
                table: "Votos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ZonaHoraria_PaisCodigo",
                table: "ZonaHoraria",
                column: "PaisCodigo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Aplicaciones");

            migrationBuilder.DropTable(
                name: "ArticleRatings");

            migrationBuilder.DropTable(
                name: "BannersInicio");

            migrationBuilder.DropTable(
                name: "contenidoCondicionesRelacion");

            migrationBuilder.DropTable(
                name: "contenidosCalificacion_ArticulosPreguntas");

            migrationBuilder.DropTable(
                name: "contenidosCalificacion_Respuestas");

            migrationBuilder.DropTable(
                name: "contenidosCategoriasRelacion");

            migrationBuilder.DropTable(
                name: "contenidoSintomasRelacion");

            migrationBuilder.DropTable(
                name: "contenidosPreguntasRelacion");

            migrationBuilder.DropTable(
                name: "contenidosRelacionados");

            migrationBuilder.DropTable(
                name: "contenidosRespuestasRelacion");

            migrationBuilder.DropTable(
                name: "contenidoTratamientosRelacion");

            migrationBuilder.DropTable(
                name: "EstadoAnimoUsuario");

            migrationBuilder.DropTable(
                name: "estudiosLabUsuario");

            migrationBuilder.DropTable(
                name: "Etiquetas");

            migrationBuilder.DropTable(
                name: "GlossaryTermMedicalLink");

            migrationBuilder.DropTable(
                name: "GlossaryValidation");

            migrationBuilder.DropTable(
                name: "NotificationSubscriptions");

            migrationBuilder.DropTable(
                name: "PreguntaCondiciones");

            migrationBuilder.DropTable(
                name: "PreguntaEtiquetas");

            migrationBuilder.DropTable(
                name: "PreguntaSintomas");

            migrationBuilder.DropTable(
                name: "PreguntaTratamientos");

            migrationBuilder.DropTable(
                name: "PushNotifications");

            migrationBuilder.DropTable(
                name: "RespuestaAIFeedback");

            migrationBuilder.DropTable(
                name: "SintomaCondicionUsuario");

            migrationBuilder.DropTable(
                name: "SintomasNotas");

            migrationBuilder.DropTable(
                name: "TrackingSintomaUsuario");

            migrationBuilder.DropTable(
                name: "TratamientoCondicionUsuario");

            migrationBuilder.DropTable(
                name: "TratamientoSintomaUsuario");

            migrationBuilder.DropTable(
                name: "TratamientosNotas");

            migrationBuilder.DropTable(
                name: "Votos");

            migrationBuilder.DropTable(
                name: "contenidosRespuestas");

            migrationBuilder.DropTable(
                name: "contenidosCategorias");

            migrationBuilder.DropTable(
                name: "estudiosLab");

            migrationBuilder.DropTable(
                name: "GlossaryTerm");

            migrationBuilder.DropTable(
                name: "Respuestas");

            migrationBuilder.DropTable(
                name: "condicionUsuario");

            migrationBuilder.DropTable(
                name: "sintomasUsuario");

            migrationBuilder.DropTable(
                name: "tratamientoUsuario");

            migrationBuilder.DropTable(
                name: "contenidos");

            migrationBuilder.DropTable(
                name: "Preguntas");

            migrationBuilder.DropTable(
                name: "sintomas");

            migrationBuilder.DropTable(
                name: "condiciones");

            migrationBuilder.DropTable(
                name: "tratamientos");

            migrationBuilder.DropTable(
                name: "Perfil");

            migrationBuilder.DropTable(
                name: "ZonaHoraria");

            migrationBuilder.DropTable(
                name: "Paises");

            migrationBuilder.DropColumn(
                name: "RequiresPasswordReset",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "RoleId",
                table: "AspNetUserRoles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserRoles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserClaims",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "AspNetRoles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "RoleId",
                table: "AspNetRoleClaims",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
