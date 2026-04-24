using eiibd26.Services.Export;
using eiibd26.Services.Export.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace eiibd26.Areas.Identity.Pages.Usuario;

[Authorize]
public class ResumenMedicoModel : PageModel
{
    private readonly IMedicalSummaryService _summaryService;
    private readonly IPdfGeneratorService _pdfService;
    private readonly ILogger<ResumenMedicoModel> _logger;

    public ResumenMedicoModel(
        IMedicalSummaryService summaryService,
        IPdfGeneratorService pdfService,
        ILogger<ResumenMedicoModel> logger)
    {
        _summaryService = summaryService;
        _pdfService     = pdfService;
        _logger         = logger;
    }

    /// <summary>
    /// Genera y descarga el PDF del resumen médico para el rango indicado.
    /// El userId se obtiene siempre del claim de autenticación.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        // Normalizar horas para incluir el día completo
        desde = desde.Date;
        hasta = hasta.Date.AddDays(1).AddTicks(-1);

        if (desde > hasta)
            return BadRequest("Rango de fechas inválido.");

        try
        {
            var dto  = await _summaryService.GenerarAsync(userId, desde, hasta, ct);
            var pdf  = _pdfService.GenerarPdf(dto);

            var nombrePersona = string.IsNullOrWhiteSpace(dto.Perfil.NombreCompleto)
                ? "Paciente"
                : dto.Perfil.NombreCompleto.Replace(" ", "_");
            var uid    = Guid.NewGuid().ToString("N")[..8];
            var nombre = $"eiibd_{nombrePersona}_{desde:yyyyMMdd}_{uid}.pdf";

            return File(pdf, "application/pdf", nombre);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parámetros inválidos al generar resumen médico para usuario {UserId}.", userId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando resumen médico para usuario {UserId}.", userId);
            return StatusCode(500, "Error al generar el resumen médico.");
        }
    }
}
