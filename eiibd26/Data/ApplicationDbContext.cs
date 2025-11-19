using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models;
using System;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { }

    // Existing / domain DbSets
    public DbSet<Aplicaciones> Aplicaciones { get; set; }
    public DbSet<condiciones> condiciones { get; set; }
    public DbSet<condicionUsuario> condicionUsuario { get; set; }
    public DbSet<estudiosLab> estudiosLab { get; set; }
    public DbSet<estudiosLabUsuario> estudiosLabUsuario { get; set; }
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
    public DbSet<Pregunta> Preguntas { get; set; }
    public DbSet<Respuesta> Respuestas { get; set; }
    public DbSet<Voto> Votos { get; set; }
    public DbSet<PreguntaEtiqueta> PreguntaEtiquetas { get; set; }
    public DbSet<Etiqueta> Etiquetas { get; set; }

    // New: contenidos module entities
    public DbSet<Contenido> Contenidos { get; set; }
    public DbSet<ContenidoRespuesta> ContenidosRespuestas { get; set; }
    public DbSet<ContenidoCategoria> ContenidosCategorias { get; set; }
    public DbSet<ContenidoCategoriaRelacion> ContenidosCategoriasRelacion { get; set; }
    public DbSet<ContenidoRelacionado> ContenidosRelacionados { get; set; }
    public DbSet<ContenidoCalificacionArticuloPregunta> ContenidosCalificacionArticulosPreguntas { get; set; }
    public DbSet<ContenidoCalificacionRespuesta> ContenidosCalificacionRespuestas { get; set; }
    public DbSet<ContenidoPreguntaRelacion> ContenidosPreguntasRelacion { get; set; }
    public DbSet<ContenidoRespuestaRelacion> ContenidosRespuestasRelacion { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Recursividad condiciones (Padre-Hijo)
        builder.Entity<condiciones>()
            .HasOne(x => x.Padre)
            .WithMany(x => x.Hijos)
            .HasForeignKey(x => x.idPadre)
            .OnDelete(DeleteBehavior.Restrict);

        // Recursividad tratamientos (Padre-Hijo)
        builder.Entity<tratamientos>()
            .HasOne(x => x.Padre)
            .WithMany(x => x.Hijos)
            .HasForeignKey(x => x.idPadre)
            .OnDelete(DeleteBehavior.Restrict);

        // Perfil - ZonaHoraria relación (si defines el modelo ZonaHoraria)
        builder.Entity<Perfil>()
            .HasOne<ZonaHoraria>()
            .WithMany()
            .HasForeignKey(p => p.idZone)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // ZonaHoraria - Paises relación
        builder.Entity<ZonaHoraria>()
            .HasOne<Paises>()
            .WithMany(p => p.ZonasHorarias)
            .HasForeignKey(z => z.PaisCodigo)
            .HasPrincipalKey(p => p.PaisCodigo)
            .OnDelete(DeleteBehavior.Restrict);

        // TrackingSintomaUsuario: relaciones
        builder.Entity<TrackingSintomaUsuario>()
            .HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.IdUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TrackingSintomaUsuario>()
            .HasOne(x => x.SintomaUsuario)
            .WithMany()
            .HasForeignKey(x => x.IdSintomaUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        // Preguntas / Respuestas / Votos / Etiquetas existing config
        builder.Entity<Pregunta>(b =>
        {
            b.ToTable("Preguntas");
            b.HasKey(p => p.Id);
            b.HasMany(p => p.Respuestas).WithOne(r => r.Pregunta).HasForeignKey(r => r.PreguntaId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(p => p.UsuarioId);
            b.HasIndex(p => p.FechaCreacion);

            // filtro global para soft-delete
            b.HasQueryFilter(p => !p.Eliminado);
        });

        builder.Entity<Respuesta>(b =>
        {
            b.ToTable("Respuestas");
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.PreguntaId);
            b.HasIndex(r => r.UsuarioId);

            b.HasQueryFilter(r => !r.Eliminado);
        });

        builder.Entity<Voto>(b =>
        {
            b.ToTable("Votos");
            b.HasKey(v => v.Id);
            b.HasIndex(v => new { v.EntidadTipo, v.EntidadId });
            b.HasIndex(v => v.UsuarioId);
            b.HasIndex(v => new { v.EntidadTipo, v.EntidadId, v.UsuarioId }).IsUnique(); // único voto por usuario por entidad

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


        /********************************************************
         * Contenidos module mapping
         ********************************************************/

        // contenidos
        builder.Entity<Contenido>(b =>
        {
            b.ToTable("contenidos");
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.IdAutor);
            b.HasIndex(c => c.IdUser);
            b.HasIndex(c => c.ContenidoFechaInicio);
            b.HasIndex(c => c.ContenidoFechaFin);

            // sanity defaults are defined at DB level; in EF enforce soft-delete query filter
            b.HasQueryFilter(c => !c.Eliminado);

            // relaciones con entidades de contenido
            b.HasMany(c => c.Respuestas)
             .WithOne(r => r.Contenido)
             .HasForeignKey(r => r.ContenidoId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(c => c.CategoriasRelacion)
             .WithOne(cr => cr.Contenido)
             .HasForeignKey(cr => cr.IdContenido)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(c => c.ContenidosRelacionados)
             .WithOne(cr => cr.Contenido)
             .HasForeignKey(cr => cr.IdContenido)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(c => c.PreguntasRelacion)
             .WithOne(cp => cp.Contenido)
             .HasForeignKey(cp => cp.ContenidoId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(c => c.RespuestasRelacion)
             .WithOne(cr => cr.Contenido)
             .HasForeignKey(cr => cr.ContenidoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // contenidosRespuestas
        builder.Entity<ContenidoRespuesta>(b =>
        {
            b.ToTable("contenidosRespuestas");
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.ContenidoId);
            b.HasIndex(r => r.IdAutor);

            b.HasQueryFilter(r => !r.Eliminado);

            b.HasOne(r => r.Contenido)
             .WithMany(c => c.Respuestas)
             .HasForeignKey(r => r.ContenidoId)
             .OnDelete(DeleteBehavior.Cascade);

            // Autor profile FK if Perfil exists
            b.HasOne(r => r.AutorPerfil)
             .WithMany()
             .HasForeignKey(r => r.IdAutor)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // contenidosCategorias
        builder.Entity<ContenidoCategoria>(b =>
        {
            b.ToTable("contenidosCategorias");
            b.HasKey(c => c.Sequence);
            b.HasIndex(c => c.Nombre);
            b.HasOne(c => c.Padre)
             .WithMany()
             .HasForeignKey(c => c.CategoriaPadre)
             .OnDelete(DeleteBehavior.Restrict);

            b.Property(c => c.FechaCreacion).HasDefaultValueSql("getdate()");
            b.Property(c => c.FechaModificacion).HasDefaultValueSql("getdate()");
        });

        // contenidosCategoriasRelacion
        builder.Entity<ContenidoCategoriaRelacion>(b =>
        {
            b.ToTable("contenidosCategoriasRelacion");
            b.HasKey(cr => cr.Sequence);
            b.HasIndex(cr => cr.IdContenido);
            b.HasIndex(cr => cr.IdCategoria);

            b.HasOne(cr => cr.Categoria)
             .WithMany()
             .HasForeignKey(cr => cr.IdCategoria)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(cr => cr.Contenido)
             .WithMany(c => c.CategoriasRelacion)
             .HasForeignKey(cr => cr.IdContenido)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // contenidosRelacionados
        builder.Entity<ContenidoRelacionado>(b =>
        {
            b.ToTable("contenidosRelacionados");
            b.HasKey(r => r.Sequence);
            b.HasIndex(r => r.IdContenido);
            b.HasIndex(r => r.IdContenidoRelacionado);

            b.HasOne(r => r.Contenido)
             .WithMany(c => c.ContenidosRelacionados)
             .HasForeignKey(r => r.IdContenido)
             .OnDelete(DeleteBehavior.Cascade);

            // self-referencing related content
            b.HasOne(r => r.ContenidosRelacionados)
             .WithMany()
             .HasForeignKey(r => r.IdContenidoRelacionado)
             .OnDelete(DeleteBehavior.Restrict);

            b.Property(r => r.FechaCreacion).HasDefaultValueSql("getdate()");
            b.Property(r => r.FechaModificacion).HasDefaultValueSql("getdate()");
        });

        // contenidosCalificacion_ArticulosPreguntas
        builder.Entity<ContenidoCalificacionArticuloPregunta>(b =>
        {
            b.ToTable("contenidosCalificacion_ArticulosPreguntas");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.IdContenido);

            b.HasOne(x => x.Contenido)
             .WithMany(c => c.CalificacionesArticulosPreguntas)
             .HasForeignKey(x => x.IdContenido)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // contenidosCalificacion_Respuestas
        builder.Entity<ContenidoCalificacionRespuesta>(b =>
        {
            b.ToTable("contenidosCalificacion_Respuestas");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.IdContenidoRespuesta);

            b.HasOne(x => x.ContenidoRespuesta)
             .WithMany()
             .HasForeignKey(x => x.IdContenidoRespuesta)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // contenidosPreguntasRelacion
        builder.Entity<ContenidoPreguntaRelacion>(b =>
        {
            b.ToTable("contenidosPreguntasRelacion");
            b.HasKey(x => x.Sequence);
            b.HasIndex(x => x.ContenidoId).HasDatabaseName("IX_contenidosPreguntasRelacion_idContenido");
            b.HasIndex(x => x.PreguntaId).HasDatabaseName("IX_contenidosPreguntasRelacion_PreguntaId");
            b.HasIndex(x => new { x.ContenidoId, x.PreguntaId }).IsUnique().HasDatabaseName("UQ_contenidosPreguntasRelacion_Content_Pregunta");

            b.HasOne(x => x.Contenido)
             .WithMany(c => c.PreguntasRelacion)
             .HasForeignKey(x => x.ContenidoId)
             .OnDelete(DeleteBehavior.Cascade);

            // Link to Preguntas table (exists in model set)
            b.HasOne<Pregunta>()
             .WithMany()
             .HasForeignKey(x => x.PreguntaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // contenidosRespuestasRelacion
        builder.Entity<ContenidoRespuestaRelacion>(b =>
        {
            b.ToTable("contenidosRespuestasRelacion");
            b.HasKey(x => x.Sequence);
            b.HasIndex(x => x.ContenidoId).HasDatabaseName("IX_contenidosRespuestasRelacion_idContenido");
            b.HasIndex(x => x.RespuestaId).HasDatabaseName("IX_contenidosRespuestasRelacion_RespuestaId");
            b.HasIndex(x => new { x.ContenidoId, x.RespuestaId }).IsUnique().HasDatabaseName("UQ_contenidosRespuestasRelacion_Content_Respuesta");

            b.HasOne(x => x.Contenido)
             .WithMany(c => c.RespuestasRelacion)
             .HasForeignKey(x => x.ContenidoId)
             .OnDelete(DeleteBehavior.Cascade);

            // Link to Respuestas table
            b.HasOne<Respuesta>()
             .WithMany()
             .HasForeignKey(x => x.RespuestaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        /********************************************************
         * End contenidos mapping
         ********************************************************/
    }

    // Soft-delete helper (puedes reutilizar)
    public void SoftDeleteEntity<T>(T entity) where T : class
    {
        // ejemplo sencillo: set property Eliminado via reflection
        var prop = entity.GetType().GetProperty("Eliminado");
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            prop.SetValue(entity, true);
        }
    }
}