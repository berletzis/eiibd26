using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { }

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