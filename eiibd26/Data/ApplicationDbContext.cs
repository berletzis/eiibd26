using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models;
using System;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Existing domain DbSets (tal como los tenías)
    public DbSet<Aplicaciones> Aplicaciones { get; set; }
    public DbSet<condiciones> condiciones { get; set; }
    public DbSet<condicionUsuario> condicionUsuario { get; set; }
    public DbSet<estudiosLab> estudiosLab { get; set; }
    public DbSet<estudiosLabUsuario> estudiosLabUsuario { get; set; }
    public DbSet<LaboratoryType> LaboratoryTypes { get; set; }
    public DbSet<PatientLaboratoryResult> PatientLaboratoryResults { get; set; }
    public DbSet<LaboratoryUnitCatalog> LaboratoryUnitCatalog { get; set; }
    public DbSet<sintomas> sintomas { get; set; }
    public DbSet<sintomasUsuario> sintomasUsuario { get; set; }
    public DbSet<tratamientos> tratamientos { get; set; }
    public DbSet<tratamientoUsuario> tratamientoUsuario { get; set; }
    public DbSet<TratamientoCondicionUsuario> TratamientoCondicionUsuario { get; set; }
    public DbSet<TratamientoSintomaUsuario> TratamientoSintomaUsuario { get; set; }
    public DbSet<Perfil> Perfil { get; set; }
    public DbSet<SintomaCondicionUsuario> SintomaCondicionUsuario { get; set; }
    public DbSet<ZonaHoraria> ZonaHoraria { get; set; }
    public DbSet<Paises> Paises { get; set; }
    public DbSet<EstadoAnimoUsuario> EstadoAnimoUsuario { get; set; }
    public DbSet<TrackingSintomaUsuario> TrackingSintomaUsuario { get; set; }
    public DbSet<FrecuenciaSintomaCatalog> FrecuenciaSintomaCatalog { get; set; }
    public DbSet<Pregunta> Preguntas { get; set; }
    public DbSet<Respuesta> Respuestas { get; set; }
    public DbSet<Voto> Votos { get; set; }
    public DbSet<PreguntaEtiqueta> PreguntaEtiquetas { get; set; }
    public DbSet<Etiqueta> Etiquetas { get; set; }

    // Contenidos
    public DbSet<Contenido> Contenidos { get; set; }
    public DbSet<ContenidoRespuesta> ContenidosRespuestas { get; set; }
    public DbSet<ContenidoCategoria> ContenidosCategorias { get; set; }
    public DbSet<ContenidoCategoriaRelacion> ContenidosCategoriasRelacion { get; set; }
    public DbSet<ContenidoRelacionado> ContenidosRelacionados { get; set; }
    public DbSet<ContenidoCalificacionArticuloPregunta> ContenidosCalificacionArticulosPreguntas { get; set; }
    public DbSet<ContenidoCalificacionRespuesta> ContenidosCalificacionRespuestas { get; set; }
    public DbSet<ContenidoPreguntaRelacion> ContenidosPreguntasRelacion { get; set; }
    public DbSet<ContenidoRespuestaRelacion> ContenidosRespuestasRelacion { get; set; }
    public DbSet<ContenidoCondicion> ContenidoCondiciones { get; set; }
    public DbSet<ContenidoSintoma> ContenidoSintomas { get; set; }
    public DbSet<ContenidoTratamiento> ContenidoTratamientos { get; set; }

    // NUEVO: tablas puente Pregunta-*
    public DbSet<PreguntaCondicion> PreguntaCondiciones { get; set; }
    public DbSet<PreguntaSintoma> PreguntaSintomas { get; set; }
    public DbSet<PreguntaTratamiento> PreguntaTratamientos { get; set; }

    public DbSet<BannerInicio> BannersInicio { get; set; }

    // PWA Push Notifications
    public DbSet<NotificationSubscription> NotificationSubscriptions { get; set; }
    public DbSet<PushNotification> PushNotifications { get; set; }

    // AI Feedback
    public DbSet<RespuestaAIFeedback> RespuestaAIFeedbacks { get; set; }

    // AI Request Logs
    public DbSet<AIRequestLog> AIRequestLogs { get; set; }

    // Article Ratings
    public DbSet<ArticleRating> ArticleRatings { get; set; }

    // Glossary Term Ratings
    public DbSet<eiibd26.Models.Glossary.GlossaryTermRating> GlossaryTermRatings { get; set; }

    // ⭐ GLOSSARY MODULE (Desacoplado - solo índice de navegación)
    public DbSet<eiibd26.Models.Glossary.GlossaryTerm> GlossaryTerms { get; set; }

    // Campañas de email por fases
    public DbSet<EmailCampanaLog> EmailCampanaLogs { get; set; }
    public DbSet<eiibd26.Models.Glossary.GlossaryTermMedicalLink> GlossaryTermMedicalLinks { get; set; }
    public DbSet<eiibd26.Models.Glossary.GlossaryValidation> GlossaryValidations { get; set; }

    // Directorio comunitario de médicos EII
    public DbSet<eiibd26.Models.Directorio.MedicoDirectorio> MedicosDirectorio { get; set; }
    public DbSet<eiibd26.Models.Directorio.AreaExperienciaEii> AreasExperienciaEii { get; set; }
    public DbSet<eiibd26.Models.Directorio.MedicoExperienciaEii> MedicoExperienciaEii { get; set; }
    public DbSet<eiibd26.Models.Directorio.TipoConfirmacion> TiposConfirmacion { get; set; }
    public DbSet<eiibd26.Models.Directorio.ConfirmacionComunitaria> ConfirmacionesComunitarias { get; set; }
    public DbSet<eiibd26.Models.Directorio.DirectorioMedicoConfirmacion> DirectorioMedicoConfirmaciones { get; set; }

    // Medico Perfil System
    public DbSet<eiibd26.Models.Medico.MedicoPerfilExtendido> MedicosPerfilExtendido { get; set; }
    public DbSet<eiibd26.Models.Medico.MedicoBadge> MedicosBadge { get; set; }
    public DbSet<eiibd26.Models.Medico.MedicoPerfilBadge> MedicosPerfilBadge { get; set; }
    public DbSet<eiibd26.Models.Medico.MedicoReclamacionToken> MedicosReclamacionToken { get; set; }
    public DbSet<eiibd26.Models.Medico.MedicoAreaEii> MedicoAreasEii { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Recursividad condiciones
        builder.Entity<condiciones>()
            .HasOne(x => x.Padre)
            .WithMany(x => x.Hijos)
            .HasForeignKey(x => x.idPadre)
            .OnDelete(DeleteBehavior.Restrict);

        // Recursividad tratamientos
        builder.Entity<tratamientos>()
            .HasOne(x => x.Padre)
            .WithMany(x => x.Hijos)
            .HasForeignKey(x => x.idPadre)
            .OnDelete(DeleteBehavior.Restrict);

        // Perfil - ZonaHoraria
        builder.Entity<Perfil>()
            .HasOne<ZonaHoraria>()
            .WithMany()
            .HasForeignKey(p => p.idZone)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ZonaHoraria>()
            .HasOne<Paises>()
            .WithMany(p => p.ZonasHorarias)
            .HasForeignKey(z => z.PaisCodigo)
            .HasPrincipalKey(p => p.PaisCodigo)
            .OnDelete(DeleteBehavior.Restrict);

        // TrackingSintomaUsuario
        builder.Entity<TrackingSintomaUsuario>()
            .HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TrackingSintomaUsuario>()
            .HasOne(x => x.SintomaUsuario).WithMany().HasForeignKey(x => x.IdSintomaUsuario).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TrackingSintomaUsuario>()
            .HasOne(x => x.Frecuencia).WithMany().HasForeignKey(x => x.FrecuenciaId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        // FrecuenciaSintomaCatalog — tabla catálogo con seed inmutable
        builder.Entity<FrecuenciaSintomaCatalog>(b =>
        {
            b.ToTable("FrecuenciaSintomaCatalog");
            b.HasKey(f => f.Id);
            b.Property(f => f.Nombre).HasMaxLength(100).IsRequired();
            b.HasData(
                new FrecuenciaSintomaCatalog { Id = 1, Nombre = "1 vez al día",          Orden = 1 },
                new FrecuenciaSintomaCatalog { Id = 2, Nombre = "2–3 veces al día",       Orden = 2 },
                new FrecuenciaSintomaCatalog { Id = 3, Nombre = "4–6 veces al día",       Orden = 3 },
                new FrecuenciaSintomaCatalog { Id = 4, Nombre = "Más de 6 veces al día",  Orden = 4 },
                new FrecuenciaSintomaCatalog { Id = 5, Nombre = "Ocasional (no diario)",  Orden = 5 },
                new FrecuenciaSintomaCatalog { Id = 6, Nombre = "Intermitente",            Orden = 6 },
                new FrecuenciaSintomaCatalog { Id = 7, Nombre = "Constante",               Orden = 7 }
            );
        });

        // Pregunta
        builder.Entity<Pregunta>(b =>
        {
            b.ToTable("Preguntas");
            b.HasKey(p => p.Id);
            b.Property(p => p.Titulo).HasMaxLength(300).IsRequired();
            b.Property(p => p.Cuerpo).IsRequired();
            b.HasIndex(p => p.UsuarioId);
            b.HasIndex(p => p.FechaCreacion);
            // DB-008: FK explícita a AspNetUsers — evita comportamiento OnDelete indefinido
            b.HasOne<ApplicationUser>()
             .WithMany()
             .HasForeignKey(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(p => !p.Eliminado);
        });

        // Respuesta
        builder.Entity<Respuesta>(b =>
        {
            b.ToTable("Respuestas");
            b.HasKey(r => r.Id);
            b.Property(r => r.Cuerpo).IsRequired();
            b.HasIndex(r => r.PreguntaId);
            b.HasIndex(r => r.UsuarioId);
            b.HasOne(r => r.Pregunta)
              .WithMany(p => p.Respuestas)
              .HasForeignKey(r => r.PreguntaId)
              .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(r => r.Parent)
              .WithMany()
              .HasForeignKey(r => r.ParentRespuestaId)
              .OnDelete(DeleteBehavior.NoAction);
            // DB-009: FK explícita a AspNetUsers — evita comportamiento OnDelete indefinido
            b.HasOne<ApplicationUser>()
             .WithMany()
             .HasForeignKey(r => r.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(r => !r.Eliminado);
        });

        builder.Entity<Voto>(b =>
        {
            b.ToTable("Votos");
            b.HasKey(v => v.Id);
            b.HasIndex(v => new { v.EntidadTipo, v.EntidadId });
            b.HasIndex(v => v.UsuarioId);
            b.HasIndex(v => new { v.EntidadTipo, v.EntidadId, v.UsuarioId }).IsUnique();
            b.HasQueryFilter(v => !v.Eliminado);
        });

        builder.Entity<PreguntaEtiqueta>(b =>
        {
            b.ToTable("PreguntaEtiquetas");
            b.HasKey(pe => pe.Id);
            b.HasIndex(pe => new { pe.PreguntaId, pe.EtiquetaId }).IsUnique();
            b.HasQueryFilter(pe => !pe.Eliminado);
        });

        builder.Entity<Etiqueta>(b =>
        {
            b.ToTable("Etiquetas");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.NombreCanonico, e.Tipo }).IsUnique();
            b.HasQueryFilter(e => !e.Eliminado);
        });

        // Contenidos (igual a versión previa)
        builder.Entity<Contenido>(b =>
        {
            b.ToTable("contenidos");
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.IdAutor);
            b.HasIndex(c => c.IdUser);
            b.HasIndex(c => c.ContenidoFechaInicio);
            b.HasIndex(c => c.ContenidoFechaFin);
            b.HasQueryFilter(c => !c.Eliminado);
            b.HasMany(c => c.Respuestas).WithOne(r => r.Contenido).HasForeignKey(r => r.ContenidoId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.CategoriasRelacion).WithOne(cr => cr.Contenido).HasForeignKey(cr => cr.IdContenido).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.ContenidosRelacionados).WithOne(cr => cr.Contenido).HasForeignKey(cr => cr.IdContenido).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.PreguntasRelacion).WithOne(cp => cp.Contenido).HasForeignKey(cp => cp.ContenidoId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.RespuestasRelacion).WithOne(cr => cr.Contenido).HasForeignKey(cr => cr.ContenidoId).OnDelete(DeleteBehavior.Cascade);
        });

        // Dentro de OnModelCreating(builder):
        builder.Entity<ContenidoCondicion>(b =>
        {
            b.ToTable("contenidoCondicionesRelacion");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.ContenidoId, x.CondicionId }).IsUnique();
            b.HasQueryFilter(x => !x.Borrado);
            b.HasOne(x => x.Contenido).WithMany().HasForeignKey(x => x.ContenidoId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Condicion).WithMany().HasForeignKey(x => x.CondicionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ContenidoSintoma>(b =>
        {
            b.ToTable("contenidoSintomasRelacion");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.ContenidoId, x.SintomaId }).IsUnique();
            b.HasQueryFilter(x => !x.Borrado);
            b.HasOne(x => x.Contenido).WithMany().HasForeignKey(x => x.ContenidoId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Sintoma).WithMany().HasForeignKey(x => x.SintomaId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ContenidoTratamiento>(b =>
        {
            b.ToTable("contenidoTratamientosRelacion");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.ContenidoId, x.TratamientoId }).IsUnique();
            b.HasQueryFilter(x => !x.Borrado);
            b.HasOne(x => x.Contenido).WithMany().HasForeignKey(x => x.ContenidoId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Tratamiento).WithMany().HasForeignKey(x => x.TratamientoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ContenidoRespuesta>(b =>
        {
            b.ToTable("contenidosRespuestas");
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.ContenidoId);
            b.HasIndex(r => r.IdAutor);
            b.HasQueryFilter(r => !r.Eliminado);
        });

        builder.Entity<ContenidoCategoria>(b =>
        {
            b.ToTable("contenidosCategorias");
            b.HasKey(c => c.Sequence);
            b.HasIndex(c => c.Nombre);
            b.HasOne(c => c.Padre).WithMany().HasForeignKey(c => c.CategoriaPadre).OnDelete(DeleteBehavior.Restrict);
            b.Property(c => c.FechaCreacion).HasDefaultValueSql("getdate()");
            b.Property(c => c.FechaModificacion).HasDefaultValueSql("getdate()");
        });

        builder.Entity<ContenidoCategoriaRelacion>(b =>
        {
            b.ToTable("contenidosCategoriasRelacion");
            b.HasKey(cr => cr.Sequence);
            b.HasIndex(cr => cr.IdContenido);
            b.HasIndex(cr => cr.IdCategoria);
        });

        builder.Entity<ContenidoRelacionado>(b =>
        {
            b.ToTable("contenidosRelacionados");
            b.HasKey(r => r.Sequence);
            b.HasIndex(r => r.IdContenido);
            b.HasIndex(r => r.IdContenidoRelacionado);
        });

        builder.Entity<ContenidoCalificacionArticuloPregunta>(b =>
        {
            b.ToTable("contenidosCalificacion_ArticulosPreguntas");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.IdContenido);
        });

        builder.Entity<ContenidoCalificacionRespuesta>(b =>
        {
            b.ToTable("contenidosCalificacion_Respuestas");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.IdContenidoRespuesta);
        });

        builder.Entity<ContenidoPreguntaRelacion>(b =>
        {
            b.ToTable("contenidosPreguntasRelacion");
            b.HasKey(x => x.Sequence);
            // mapear explícitamente la columna que en la BD es 'idContenido'
            b.Property(x => x.ContenidoId).HasColumnName("idContenido");
            b.Property(x => x.PreguntaId).HasColumnName("PreguntaId");
            b.HasIndex(x => x.ContenidoId);
            b.HasIndex(x => x.PreguntaId);
            b.HasIndex(x => new { x.ContenidoId, x.PreguntaId }).IsUnique();
            b.HasQueryFilter(x => !x.Borrado);
        });

        builder.Entity<ContenidoRespuestaRelacion>(b =>
        {
            b.ToTable("contenidosRespuestasRelacion");
            b.HasKey(x => x.Sequence);
            b.HasIndex(x => x.ContenidoId);
            b.HasIndex(x => x.RespuestaId);
            b.HasIndex(x => new { x.ContenidoId, x.RespuestaId }).IsUnique();
        });

        // Tablas puente Pregunta-*
        builder.Entity<PreguntaCondicion>(b =>
        {
            b.HasIndex(x => new { x.PreguntaId, x.CondicionId }).IsUnique();
        });
        builder.Entity<PreguntaSintoma>(b =>
        {
            b.HasIndex(x => new { x.PreguntaId, x.SintomaId }).IsUnique();
        });
        builder.Entity<PreguntaTratamiento>(b =>
        {
            b.HasIndex(x => new { x.PreguntaId, x.TratamientoId }).IsUnique();
        });

        // ArticleRating configuration
        builder.Entity<ArticleRating>(b =>
        {
            b.ToTable("ArticleRatings");
            b.HasKey(ar => ar.Id);

            // Index for article lookups
            b.HasIndex(ar => ar.ArticleId);

            // Index for user lookups
            b.HasIndex(ar => ar.UserId);

            // Unique constraint: one rating per user per article
            // For authenticated users: UserId + ArticleId must be unique
            b.HasIndex(ar => new { ar.UserId, ar.ArticleId })
             .IsUnique()
             .HasFilter("[UserId] IS NOT NULL");

            // For anonymous users: IP + ArticleId (optional, commented out by default)
            // b.HasIndex(ar => new { ar.IpAddress, ar.ArticleId })
            //  .IsUnique()
            //  .HasFilter("[IpAddress] IS NOT NULL AND [UserId] IS NULL");

            // Relationship with Contenido (Article)
            b.HasOne(ar => ar.Article)
             .WithMany()
             .HasForeignKey(ar => ar.ArticleId)
             .OnDelete(DeleteBehavior.Cascade);

            // Relationship with User (optional)
            b.HasOne(ar => ar.User)
             .WithMany()
             .HasForeignKey(ar => ar.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // GlossaryTermRating configuration
        builder.Entity<eiibd26.Models.Glossary.GlossaryTermRating>(b =>
        {
            b.ToTable("GlossaryTermRatings");
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.TermId);
            b.HasIndex(r => r.UserId);
            b.HasIndex(r => new { r.UserId, r.TermId })
             .IsUnique()
             .HasFilter("[UserId] IS NOT NULL");
            b.HasOne(r => r.Term)
             .WithMany()
             .HasForeignKey(r => r.TermId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ⭐ GLOSSARY VALIDATION: validaciones humanas acumulativas
        builder.Entity<eiibd26.Models.Glossary.GlossaryValidation>(b =>
        {
            b.ToTable("GlossaryValidation");
            b.HasKey(v => v.Id);
            b.HasIndex(v => v.GlossaryTermId);
            b.HasIndex(v => v.UserId);
            // Garantiza: un usuario vota una sola vez por tipo+nivel
            b.HasIndex(v => new { v.GlossaryTermId, v.UserId, v.ValidationType, v.MedicalRelationTypeId })
             .IsUnique()
             .HasFilter("[MedicalRelationTypeId] IS NOT NULL");
            // Garantiza: una sola validación de descripción por usuario (ValidationType=1, relación NULL)
            b.HasIndex(v => new { v.GlossaryTermId, v.UserId, v.ValidationType })
             .IsUnique()
             .HasFilter("[MedicalRelationTypeId] IS NULL");
            b.HasOne(v => v.GlossaryTerm)
             .WithMany(t => t.Validaciones)
             .HasForeignKey(v => v.GlossaryTermId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // LaboratoryType — catálogo jerárquico
        builder.Entity<LaboratoryType>(b =>
        {
            b.ToTable("LaboratoryTypes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(500);
            b.HasOne(x => x.Parent)
             .WithMany(x => x.Children)
             .HasForeignKey(x => x.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => !x.Eliminado);
        });

        // LaboratoryUnitCatalog — catálogo de unidades de medida
        builder.Entity<LaboratoryUnitCatalog>(b =>
        {
            b.ToTable("LaboratoryUnitCatalog");
            b.HasKey(u => u.Id);
            b.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
            b.Property(u => u.Abreviatura).HasMaxLength(20).IsRequired();
        });

        // PatientLaboratoryResult — resultados del paciente
        builder.Entity<PatientLaboratoryResult>(b =>
        {
            b.ToTable("PatientLaboratoryResults");
            b.HasKey(x => x.Id);
            // DB-004: índice para acelerar carga de resultados por paciente
            b.HasIndex(x => x.PatientId)
             .HasDatabaseName("IX_PatientLaboratoryResults_PatientId");
            b.Property(x => x.ResultValue).HasMaxLength(500);
            b.Property(x => x.ResultUnit).HasMaxLength(50);
            b.Property(x => x.Notes).HasMaxLength(1000);
            b.HasOne(x => x.LaboratoryType)
             .WithMany(x => x.Results)
             .HasForeignKey(x => x.LaboratoryTypeId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.LaboratoryUnit)
             .WithMany()
             .HasForeignKey(x => x.LaboratoryUnitCatalogId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.CondicionUsuario)
             .WithMany()
             .HasForeignKey(x => x.CondicionUsuarioId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.SintomaUsuario)
             .WithMany()
             .HasForeignKey(x => x.SintomaUsuarioId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.TratamientoUsuario)
             .WithMany()
             .HasForeignKey(x => x.TratamientoUsuarioId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => !x.Eliminado);
        });

        // EmailCampanaLog: índice para evitar duplicados exitosos (UserId, Fase) y acelerar consultas
        builder.Entity<EmailCampanaLog>(b =>
        {
            b.ToTable("EmailCampanaLog");
            b.HasIndex(e => new { e.UserId, e.Fase, e.Exito })
             .HasDatabaseName("IX_EmailCampanaLog_UserId_Fase_Exito");
            b.HasIndex(e => e.FechaEnvio)
             .HasDatabaseName("IX_EmailCampanaLog_FechaEnvio");
        });

        // DB-004: índice en NotificationSubscription.UserId para acelerar lookup de suscripciones por usuario
        builder.Entity<NotificationSubscription>(b =>
        {
            b.ToTable("NotificationSubscriptions");
            b.HasIndex(x => x.UserId)
             .HasDatabaseName("IX_NotificationSubscriptions_UserId");
        });

        // ── DIRECTORIO MÉDICOS EII ──────────────────────────────────────────────

        // ── ÍNDICES TABLAS CLÍNICAS (DB-001/002/003) ─────────────────────────────
        // Consultas del dashboard y perfil siempre filtran por IdUsuario.
        // Sin estos índices cada query hace full scan sobre tablas de gran volumen.

        builder.Entity<condicionUsuario>(b =>
        {
            b.HasIndex(x => x.idUsuario)
             .HasDatabaseName("IX_condicionUsuario_IdUsuario");
        });

        builder.Entity<sintomasUsuario>(b =>
        {
            b.HasIndex(x => x.idUsuario)
             .HasDatabaseName("IX_sintomasUsuario_IdUsuario");
        });

        builder.Entity<tratamientoUsuario>(b =>
        {
            b.HasIndex(x => x.idUsuario)
             .HasDatabaseName("IX_tratamientoUsuario_IdUsuario");
        });

        builder.Entity<EstadoAnimoUsuario>(b =>
        {
            b.HasIndex(x => x.IdUsuario)
             .HasDatabaseName("IX_EstadoAnimoUsuario_IdUsuario");
            b.HasIndex(x => new { x.IdUsuario, x.FechaRegistro })
             .HasDatabaseName("IX_EstadoAnimoUsuario_IdUsuario_FechaRegistro");
        });

        builder.Entity<TrackingSintomaUsuario>(b =>
        {
            // IX_TrackingSintomaUsuario_IdUsuario ya viene de la config de arriba,
            // pero agregarmos el índice compuesto para consultas de rango de fechas.
            b.HasIndex(x => new { x.IdUsuario, x.Fecha })
             .HasDatabaseName("IX_TrackingSintomaUsuario_IdUsuario_Fecha");
        });

        // DB-003: índices en tablas de correlación clínica — consultas siempre filtran por IdUsuario
        builder.Entity<TratamientoSintomaUsuario>(b =>
        {
            b.HasIndex(x => x.IdUsuario)
             .HasDatabaseName("IX_TratamientoSintomaUsuario_IdUsuario");
        });

        builder.Entity<SintomaCondicionUsuario>(b =>
        {
            b.HasIndex(x => x.IdUsuario)
             .HasDatabaseName("IX_SintomaCondicionUsuario_IdUsuario");
        });

        // ── FIN ÍNDICES TABLAS CLÍNICAS ───────────────────────────────────────

        builder.Entity<eiibd26.Models.Directorio.AreaExperienciaEii>(b =>
        {
            b.ToTable("AreaExperienciaEii");
            b.HasKey(a => a.Id);
            b.Property(a => a.Nombre).HasMaxLength(100).IsRequired();
            b.Property(a => a.Descripcion).HasMaxLength(300);
            b.HasData(
                new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 1,  Nombre = "CUCI",                   Orden = 1,  Activo = true },
                new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 2,  Nombre = "Crohn",                  Orden = 2,  Activo = true },
                new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 3,  Nombre = "Pediátrico",             Orden = 3,  Activo = true },
                new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 4,  Nombre = "Ostomías",               Orden = 4,  Activo = true },
                new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 5,  Nombre = "Biológicos",             Orden = 5,  Activo = true },
                new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 6,  Nombre = "Embarazo + EII",         Orden = 6,  Activo = true },
                new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 7,  Nombre = "Manejo de brotes",       Orden = 7,  Activo = true },
                new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 8,  Nombre = "Segunda opinión",        Orden = 8,  Activo = true },
                new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 9,  Nombre = "Cirugía",                Orden = 9,  Activo = true },
                new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 10, Nombre = "Seguimiento prolongado", Orden = 10, Activo = true }
            );
        });

        builder.Entity<eiibd26.Models.Directorio.TipoConfirmacion>(b =>
        {
            b.ToTable("TipoConfirmacion");
            b.HasKey(t => t.Id);
            b.Property(t => t.Nombre).HasMaxLength(100).IsRequired();
            b.Property(t => t.Descripcion).HasMaxLength(300);
            b.HasData(
                new eiibd26.Models.Directorio.TipoConfirmacion { Id = 1, Nombre = "Me diagnosticó",                     Orden = 1, Activo = true },
                new eiibd26.Models.Directorio.TipoConfirmacion { Id = 2, Nombre = "Me ayudó con tratamiento biológico", Orden = 2, Activo = true },
                new eiibd26.Models.Directorio.TipoConfirmacion { Id = 3, Nombre = "Manejo de brotes",                   Orden = 3, Activo = true },
                new eiibd26.Models.Directorio.TipoConfirmacion { Id = 4, Nombre = "Segunda opinión",                    Orden = 4, Activo = true },
                new eiibd26.Models.Directorio.TipoConfirmacion { Id = 5, Nombre = "Cirugía",                            Orden = 5, Activo = true },
                new eiibd26.Models.Directorio.TipoConfirmacion { Id = 6, Nombre = "Seguimiento prolongado",             Orden = 6, Activo = true }
            );
        });

        builder.Entity<eiibd26.Models.Directorio.MedicoDirectorio>(b =>
        {
            b.ToTable("MedicosDirectorio");
            b.HasKey(m => m.Id);
            b.Property(m => m.NombreCompleto).HasMaxLength(300).IsRequired();
            b.Property(m => m.CedulaProfesional).HasMaxLength(20);
            b.Property(m => m.Especialidad).HasMaxLength(200);
            b.Property(m => m.Subespecialidad).HasMaxLength(200);
            b.Property(m => m.Estado).HasMaxLength(100);
            b.Property(m => m.Ciudad).HasMaxLength(100);
            b.Property(m => m.MunicipioAlcaldia).HasMaxLength(100);
            b.Property(m => m.HospitalClinica).HasMaxLength(300);
            b.Property(m => m.Latitud).HasColumnType("decimal(9,6)");
            b.Property(m => m.Longitud).HasColumnType("decimal(9,6)");
            b.Property(m => m.EstatusValidacion).HasConversion<int>();
            b.Property(m => m.NivelConfianza).HasConversion<int>();
            b.Property(m => m.EstatusReclamacion).HasConversion<int>();
            b.HasQueryFilter(m => !m.Eliminado);
            b.HasIndex(m => m.CedulaProfesional);
            b.HasIndex(m => new { m.Estado, m.Ciudad });
            // DB-015: índice en AspNetUserId para acelerar lookup del médico por usuario vinculado
            b.HasIndex(m => m.AspNetUserId)
             .HasDatabaseName("IX_MedicosDirectorio_AspNetUserId")
             .HasFilter("[AspNetUserId] IS NOT NULL");
            b.HasOne(m => m.UsuarioVinculado)
             .WithMany()
             .HasForeignKey(m => m.AspNetUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<eiibd26.Models.Directorio.MedicoExperienciaEii>(b =>
        {
            b.ToTable("MedicoExperienciaEii");
            b.HasKey(me => me.Id);
            b.HasIndex(me => new { me.MedicoDirectorioId, me.AreaExperienciaEiiId })
             .IsUnique()
             .HasFilter("[Eliminado] = 0");
            b.HasQueryFilter(me => !me.Eliminado);
            b.HasOne(me => me.MedicoDirectorio)
             .WithMany(m => m.AreasExperiencia)
             .HasForeignKey(me => me.MedicoDirectorioId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(me => me.AreaExperienciaEii)
             .WithMany(a => a.MedicosExperiencia)
             .HasForeignKey(me => me.AreaExperienciaEiiId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<eiibd26.Models.Directorio.ConfirmacionComunitaria>(b =>
        {
            b.ToTable("ConfirmacionComunitaria");
            b.HasKey(c => c.Id);
            b.HasIndex(c => new { c.MedicoDirectorioId, c.UsuarioId, c.TipoConfirmacionId })
             .IsUnique()
             .HasFilter("[Eliminado] = 0");
            b.HasQueryFilter(c => !c.Eliminado);
            b.HasOne(c => c.MedicoDirectorio)
             .WithMany(m => m.Confirmaciones)
             .HasForeignKey(c => c.MedicoDirectorioId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(c => c.TipoConfirmacion)
             .WithMany(t => t.Confirmaciones)
             .HasForeignKey(c => c.TipoConfirmacionId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(c => c.Usuario)
             .WithMany()
             .HasForeignKey(c => c.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<eiibd26.Models.Medico.MedicoPerfilExtendido>(b =>
        {
            b.ToTable("MedicoPerfilExtendido");
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.MedicoId).IsUnique().HasFilter("[MedicoId] IS NOT NULL");
            b.HasIndex(p => p.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
            b.HasIndex(p => p.UserId);
            b.HasOne(p => p.Medico)
             .WithMany()
             .HasForeignKey(p => p.MedicoId)
             .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(p => p.Usuario)
             .WithMany()
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<eiibd26.Models.Medico.MedicoBadge>(b =>
        {
            b.ToTable("MedicoBadge");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Codigo).IsUnique();
        });

        builder.Entity<eiibd26.Models.Medico.MedicoPerfilBadge>(b =>
        {
            b.ToTable("MedicoPerfilBadge");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.MedicoId, x.BadgeId }).IsUnique();
            b.Property(x => x.OtorgadoPor).HasMaxLength(50).IsRequired();
            b.HasOne(x => x.Medico)
             .WithMany()
             .HasForeignKey(x => x.MedicoId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Badge)
             .WithMany(b2 => b2.PerfilesBadges)
             .HasForeignKey(x => x.BadgeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<eiibd26.Models.Medico.MedicoReclamacionToken>(b =>
        {
            b.ToTable("MedicoReclamacionToken");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Token).IsUnique();
            b.HasIndex(x => x.MedicoId);
            b.HasOne(x => x.Medico)
             .WithMany()
             .HasForeignKey(x => x.MedicoId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Usuario)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<eiibd26.Models.Medico.MedicoAreaEii>(b =>
        {
            b.ToTable("MedicoAreaEii");
            b.HasKey(x => new { x.MedicoPerfilId, x.CondicionId });
            b.HasOne(x => x.MedicoPerfil)
             .WithMany()
             .HasForeignKey(x => x.MedicoPerfilId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Condicion)
             .WithMany()
             .HasForeignKey(x => x.CondicionId)
             // DB-005: Restrict evita borrado masivo de experiencia médica al eliminar condición.
             // El borrado de condiciones clínicas no debe propagarse a perfiles de médicos.
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public void SoftDeleteEntity<T>(T entity) where T : class
    {
        var prop = entity.GetType().GetProperty("Eliminado");
        if (prop != null && prop.PropertyType == typeof(bool))
            prop.SetValue(entity, true);
    }
}