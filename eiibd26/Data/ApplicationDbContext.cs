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

    // ¡Agrega el nuevo DbSet!
    public DbSet<TrackingSintomaUsuario> TrackingSintomaUsuario { get; set; }

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
    }
}