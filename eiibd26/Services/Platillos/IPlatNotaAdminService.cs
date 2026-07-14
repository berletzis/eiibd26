using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eiibd26.Services.Platillos
{
    /// <summary>
    /// Escritura admin de notas clínicas. Contraparte de <see cref="IPlatNotaClinicaService"/>
    /// (que solo LEE con el candado): aquí viven las reglas de ESCRITURA, también en un solo lugar
    /// para que Ingredientes y Grupos las compartan sin duplicarlas:
    ///
    ///  · Guardar SIEMPRE deja la nota en borrador (Publicado = 0). Editar contenido de una nota
    ///    publicada la baja a borrador — regla de oro de moderación: cambió el contenido, se re-evalúa.
    ///  · Publicar exige el guard: ≥1 sección con contenido (mismo predicado que el candado de lectura).
    ///  · Publicar y despublicar son actos deliberados y aparte del guardado del contenido.
    ///
    /// Solo lo consumen páginas admin ([Authorize(Roles="Administrador")]); no es una ruta pública.
    /// </summary>
    public interface IPlatNotaAdminService
    {
        /// <summary>Carga la nota del destino para editar (o un VM vacío si no existe). Incluye borradores.</summary>
        Task<PlatNotaEditVm> CargarAsync(string tipoDestino, int destinoId, string destinoNombre);

        /// <summary>Estado de la nota por DestinoId para un tipo, en una sola consulta (columna de la lista).</summary>
        Task<Dictionary<int, PlatNotaEstado>> ObtenerEstadosAsync(string tipoDestino);

        /// <summary>
        /// Guarda contenido (upsert nota + delete-all/insert de secciones y referencias) y SIEMPRE
        /// deja Publicado = 0. Devuelve (ok, mensaje) para TempData.
        /// </summary>
        Task<(bool Ok, string Mensaje)> GuardarBorradorAsync(
            string tipoDestino, int destinoId,
            string? titulo,
            List<PlatNotaSeccionInput> secciones,
            List<PlatNotaReferenciaInput> referencias);

        /// <summary>Levanta el candado si pasa el guard (≥1 sección con contenido). Estampa quién y cuándo.</summary>
        Task<(bool Ok, string Mensaje)> PublicarAsync(string tipoDestino, int destinoId, Guid userId);

        /// <summary>Baja el candado: la nota deja de ser visible. Limpia la estampa de publicación.</summary>
        Task<(bool Ok, string Mensaje)> DespublicarAsync(string tipoDestino, int destinoId);
    }
}
