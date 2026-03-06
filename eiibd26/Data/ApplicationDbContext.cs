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

        // Pregunta
        builder.Entity<Pregunta>(b =>
        {
            b.ToTable("Preguntas");
            b.HasKey(p => p.Id);
            b.Property(p => p.Titulo).HasMaxLength(300).IsRequired();
            b.Property(p => p.Cuerpo).IsRequired();
            b.HasIndex(p => p.UsuarioId);
            b.HasIndex(p => p.FechaCreacion);
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
    }

    public void SoftDeleteEntity<T>(T entity) where T : class
    {
        var prop = entity.GetType().GetProperty("Eliminado");
        if (prop != null && prop.PropertyType == typeof(bool))
            prop.SetValue(entity, true);
    }
}