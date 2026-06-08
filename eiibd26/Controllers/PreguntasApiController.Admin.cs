using eiibd26.DTOs;
using eiibd26.Helpers;
using eiibd26.Models;
using eiibd26.Services.ShortUrl;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace eiibd26.Controllers
{
    public partial class PreguntasApiController
    {
        // POST api/preguntas/admin/crear-facebook
        [HttpPost("admin/crear-facebook")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CrearFacebook(
            [FromBody] CrearPreguntaDto dto,
            [FromServices] IConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(dto?.Titulo) || string.IsNullOrWhiteSpace(dto.Cuerpo))
                return BadRequest(new { ok = false, error = "Título y cuerpo son requeridos." });

            if (dto.Titulo.Length > 300)
                return BadRequest(new { ok = false, error = "Título muy largo (máx. 300 caracteres)." });

            var comunidadIdRaw = config["Comunidad:UserId"];
            if (!Guid.TryParse(comunidadIdRaw, out var comunidadUserId))
                return StatusCode(500, new { ok = false, error = "Comunidad:UserId no configurado." });

            try
            {
                var slug = await SlugHelper.GenerateUniqueSlugForPregunta(_db, dto.Titulo);

                var pregunta = new Pregunta
                {
                    Id            = Guid.NewGuid(),
                    Titulo        = dto.Titulo.Trim(),
                    Cuerpo        = dto.Cuerpo.Trim(),
                    Slug          = slug,
                    UsuarioId     = comunidadUserId,
                    FechaCreacion = DateTimeOffset.UtcNow,
                    Eliminado     = false,
                    Resuelta      = false
                };

                _db.Preguntas.Add(pregunta);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "[Admin/Facebook] Pregunta {Id} creada con slug '{Slug}'",
                    pregunta.Id, pregunta.Slug);

                if (dto.CondicionId.HasValue)
                {
                    try
                    {
                        var condicionExiste = await _db.condiciones
                            .AnyAsync(c => c.id == dto.CondicionId.Value && !c.Eliminado);
                        if (condicionExiste)
                        {
                            _db.PreguntaCondiciones.Add(new PreguntaCondicion
                            {
                                Id          = Guid.NewGuid(),
                                PreguntaId  = pregunta.Id,
                                CondicionId = dto.CondicionId.Value
                            });
                            await _db.SaveChangesAsync();
                        }
                    }
                    catch (Exception exCond)
                    {
                        _logger.LogWarning(exCond,
                            "[Admin/Facebook] No se pudo asociar condición {CondicionId} a pregunta {Id}",
                            dto.CondicionId.Value, pregunta.Id);
                    }
                }

                var yaExiste = await _db.Respuestas
                    .AnyAsync(r => r.PreguntaId == pregunta.Id && r.EsIA && !r.Eliminado);

                if (!yaExiste)
                {
                    var idCapture = pregunta.Id;
                    _backgroundJobs.Enqueue<eiibd26.Jobs.AiAnswerJob>(
                        job => job.ProcesarPreguntaAsync(idCapture));
                    _logger.LogInformation(
                        "[Admin/Facebook] AI job enqueued para {Id}", pregunta.Id);
                }

                return Ok(new { ok = true, id = pregunta.Id, slug = pregunta.Slug });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Admin/Facebook] Error al crear pregunta");
                return StatusCode(500, new { ok = false, error = "Error al crear la pregunta." });
            }
        }

        // POST api/preguntas/admin/{id}/shorturl
        [HttpPost("admin/{id:guid}/shorturl")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GenerarShortUrl(
            Guid id,
            [FromServices] IShortUrlService shortUrlService)
        {
            var pregunta = await _db.Preguntas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.Eliminado);

            if (pregunta is null)
                return NotFound(new { ok = false, error = "Pregunta no encontrada." });

            var siteRoot = $"{Request.Scheme}://{Request.Host}";
            var url = string.IsNullOrWhiteSpace(pregunta.Slug)
                ? $"{siteRoot}/Preguntas/Detalles?id={pregunta.Id}"
                : $"{siteRoot}/Preguntas/{pregunta.Slug}";

            try
            {
                var codigo = await shortUrlService.CrearAsync(url, origen: "facebook");
                var shortUrl = $"{siteRoot}/r/{codigo}";

                _logger.LogInformation(
                    "[Admin/Facebook] Short-URL /r/{Codigo} generado para pregunta {Id}", codigo, id);

                return Ok(new { ok = true, codigo, shortUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Admin/Facebook] Error generando short-url para {Id}", id);
                return StatusCode(500, new { ok = false, error = "Error al generar la short-URL." });
            }
        }
    }
}
