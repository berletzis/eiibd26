using System;
using System.Threading.Tasks;
using eiibd26.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    /// <summary>
    /// Panel admin SOLO LECTURA del estado de firmas de externos (tabla ScrapedPage,
    /// poblada por el Worker). No activa el Worker; solo consulta cuántos externos hay
    /// firmados / pendientes, por idioma. El "qué está haciendo ahora" y los errores en
    /// vivo se ven en el log del Worker (corre desde terminal).
    /// Lee vía SQL crudo para no acoplar el modelo EF del Web a las tablas del Worker.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class FirmasExternasModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<FirmasExternasModel> _logger;

        public FirmasExternasModel(ApplicationDbContext db, ILogger<FirmasExternasModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        public bool TablaDisponible { get; private set; } = true;
        public string? Aviso { get; private set; }

        public int Total { get; private set; }
        public int Firmados { get; private set; }
        public int Pendientes => Total - Firmados;
        public int PorcentajeFirmado => Total > 0 ? (int)Math.Round(Firmados * 100.0 / Total) : 0;

        public int EsTotal { get; private set; }
        public int EsFirmados { get; private set; }
        public int EnTotal { get; private set; }
        public int EnFirmados { get; private set; }
        public int OtrosTotal { get; private set; }
        public int OtrosFirmados { get; private set; }

        public DateTime? UltimaFirma { get; private set; }

        public async Task OnGetAsync()
        {
            try
            {
                Total = await ScalarInt("SELECT COUNT(*) AS Value FROM ScrapedPage");
                Firmados = await ScalarInt("SELECT COUNT(*) AS Value FROM ScrapedPage WHERE Firma IS NOT NULL");

                EsTotal = await ScalarInt("SELECT COUNT(*) AS Value FROM ScrapedPage WHERE Language LIKE 'es%'");
                EsFirmados = await ScalarInt("SELECT COUNT(*) AS Value FROM ScrapedPage WHERE Language LIKE 'es%' AND Firma IS NOT NULL");

                EnTotal = await ScalarInt("SELECT COUNT(*) AS Value FROM ScrapedPage WHERE Language LIKE 'en%'");
                EnFirmados = await ScalarInt("SELECT COUNT(*) AS Value FROM ScrapedPage WHERE Language LIKE 'en%' AND Firma IS NOT NULL");

                OtrosTotal = Total - EsTotal - EnTotal;
                OtrosFirmados = Firmados - EsFirmados - EnFirmados;

                UltimaFirma = await ScalarNullableDate("SELECT MAX(FirmaCalculadaEn) AS Value FROM ScrapedPage");
            }
            catch (Exception ex)
            {
                TablaDisponible = false;
                Aviso = "No se pudo leer ScrapedPage.Firma. ¿Ejecutaste SQL/alter-scrapedpage-firma.sql? Detalle: " + ex.Message;
                _logger.LogWarning(ex, "[FirmasExternas] No se pudo leer el estado de firmas de externos.");
            }
        }

        private async Task<int> ScalarInt(string sql) =>
            await _db.Database.SqlQueryRaw<int>(sql).SingleAsync();

        private async Task<DateTime?> ScalarNullableDate(string sql) =>
            await _db.Database.SqlQueryRaw<DateTime?>(sql).SingleAsync();
    }
}
