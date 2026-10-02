using eiibd_IMX.Models.ReadOnly;
using Microsoft.EntityFrameworkCore;

namespace eiibd_IMX.Data;

/// <summary>
/// Contexto de SOLO LECTURA sobre la base de datos de EIIBD.
///
/// Este proyecto nunca escribe. SaveChanges lanza excepcion a proposito: si algun
/// dia alguien agrega un Add/Update por error, el build pasa pero la ejecucion falla
/// de inmediato en vez de tocar produccion en silencio.
///
/// Hoy mapea directo a las tablas de dbo. Cuando exista el esquema `pub` con las
/// vistas, cambiar cada ToTable("x") por ToView("vw_x", "pub") y nada mas.
/// </summary>
public sealed class ImxDbContext : DbContext
{
    public ImxDbContext(DbContextOptions<ImxDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public DbSet<ContenidoRow> Contenidos => Set<ContenidoRow>();
    public DbSet<CategoriaRow> Categorias => Set<CategoriaRow>();
    public DbSet<CategoriaRelacionRow> CategoriasRelacion => Set<CategoriaRelacionRow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ContenidoRow>(e =>
        {
            e.ToTable("contenidos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("Id");
        });

        b.Entity<CategoriaRow>(e =>
        {
            e.ToTable("contenidosCategorias");
            e.HasKey(x => x.Sequence);
        });

        b.Entity<CategoriaRelacionRow>(e =>
        {
            e.ToTable("contenidosCategoriasRelacion");
            e.HasKey(x => x.Sequence);
        });
    }

    public override int SaveChanges()
        => throw new NotSupportedException("eiibd-IMX es un sitio de solo lectura.");

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
        => throw new NotSupportedException("eiibd-IMX es un sitio de solo lectura.");
}
