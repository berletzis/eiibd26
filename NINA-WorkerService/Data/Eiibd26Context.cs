using Microsoft.EntityFrameworkCore;
using NINA_WorkerService.Models;

namespace NINA_WorkerService.Data;

public class Eiibd26Context : DbContext
{
    public Eiibd26Context(DbContextOptions<Eiibd26Context> options)
        : base(options)
    {
    }

    public DbSet<SourceSite> SourceSites => Set<SourceSite>();
    public DbSet<ScrapedPage> ScrapedPages => Set<ScrapedPage>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleTranslation> ArticleTranslations => Set<ArticleTranslation>();
    public DbSet<ArticleSimilarity> ArticleSimilarities => Set<ArticleSimilarity>();
    public DbSet<ArticleSummary> ArticleSummaries => Set<ArticleSummary>();
    public DbSet<ArticleBoost> ArticleBoosts => Set<ArticleBoost>();
    public DbSet<ScrapingJob> ScrapingJobs => Set<ScrapingJob>();
    public DbSet<ScrapingJobLog> ScrapingJobLogs => Set<ScrapingJobLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mapear a nombres de tablas reales
        modelBuilder.Entity<SourceSite>().ToTable("SourceSite");
        // Metadatos de fuente (largos coinciden con SQL/alter-sourcesite-metadatos.sql)
        modelBuilder.Entity<SourceSite>().Property(s => s.Idioma).HasMaxLength(10);
        modelBuilder.Entity<SourceSite>().Property(s => s.Pais).HasMaxLength(10);
        modelBuilder.Entity<SourceSite>().Property(s => s.Categoria).HasMaxLength(100);
        modelBuilder.Entity<SourceSite>().Property(s => s.UrlPublica).HasMaxLength(500);
        modelBuilder.Entity<ScrapedPage>().ToTable("ScrapedPage");
        modelBuilder.Entity<Article>().ToTable("Article");
        modelBuilder.Entity<ArticleTranslation>().ToTable("ArticleTranslation");
        modelBuilder.Entity<ArticleSimilarity>().ToTable("ArticleSimilarity");
        modelBuilder.Entity<ArticleSummary>().ToTable("ArticleSummary");
        modelBuilder.Entity<ArticleBoost>().ToTable("ArticleBoost");
        modelBuilder.Entity<ScrapingJob>().ToTable("ScrapingJob");
        modelBuilder.Entity<ScrapingJobLog>().ToTable("ScrapingJobLog");

        // Relaciones
        modelBuilder.Entity<ScrapedPage>()
            .HasOne(sp => sp.SourceSite)
            .WithMany(ss => ss.ScrapedPages)
            .HasForeignKey(sp => sp.SourceSiteId);

        modelBuilder.Entity<Article>()
            .HasOne(a => a.ScrapedPage)
            .WithOne(sp => sp.Article)
            .HasForeignKey<Article>(a => a.ScrapedPageId);

        modelBuilder.Entity<ArticleTranslation>()
            .HasOne(at => at.Article)
            .WithMany(a => a.Translations)
            .HasForeignKey(at => at.ArticleId);

        modelBuilder.Entity<ArticleSummary>()
            .HasOne(s => s.Article)
            .WithMany(a => a.Summaries)
            .HasForeignKey(s => s.ArticleId);

        modelBuilder.Entity<ArticleBoost>()
            .HasOne(b => b.Article)
            .WithMany(a => a.Boosts)
            .HasForeignKey(b => b.ArticleId);

        modelBuilder.Entity<ArticleSimilarity>()
            .HasKey(x => new { x.ArticleId1, x.ArticleId2 });

        modelBuilder.Entity<ArticleSimilarity>()
            .HasOne(x => x.Article1)
            .WithMany(a => a.SimilaritiesAs1)
            .HasForeignKey(x => x.ArticleId1)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ArticleSimilarity>()
            .HasOne(x => x.Article2)
            .WithMany(a => a.SimilaritiesAs2)
            .HasForeignKey(x => x.ArticleId2)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ScrapingJobLog>()
            .HasOne(l => l.ScrapingJob)
            .WithMany(j => j.Logs)
            .HasForeignKey(l => l.ScrapingJobId);

        modelBuilder.Entity<ScrapingJobLog>()
            .HasOne(l => l.ScrapedPage)
            .WithMany()
            .HasForeignKey(l => l.ScrapedPageId);

        // Precisiones de decimal para evitar warnings
        modelBuilder.Entity<ArticleBoost>()
            .Property(b => b.BoostScore)
            .HasPrecision(6, 3);

        modelBuilder.Entity<ArticleSimilarity>()
            .Property(s => s.SimilarityScore)
            .HasPrecision(5, 4);

        modelBuilder.Entity<ArticleTranslation>()
            .Property(t => t.QualityScore)
            .HasPrecision(4, 3);
    }
}