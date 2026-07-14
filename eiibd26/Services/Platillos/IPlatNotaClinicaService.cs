using System.Collections.Generic;
using System.Threading.Tasks;

namespace eiibd26.Services.Platillos
{
    /// <summary>
    /// ÚNICO punto de acceso de lectura a las notas clínicas para el paciente. El candado
    /// (Publicado = 1 AND Activo = 1 AND ≥1 sección con contenido) vive aquí y en
    /// ningún otro lado: ninguna vista consulta PlatNotaClinica directo. Así es IMPOSIBLE
    /// pedir una nota sin el candado, no solo "poco probable".
    ///
    /// Regla de oro que estas firmas garantizan: nota inexistente, nota no publicada y nota
    /// publicada-pero-sin-contenido devuelven exactamente lo mismo — nada.
    /// </summary>
    public interface IPlatNotaClinicaService
    {
        /// <summary>La nota visible del destino, o null si no debe verse (candado cerrado o sin contenido).</summary>
        Task<PlatNotaVisibleDto?> ObtenerNotaVisibleParaPacienteAsync(string tipoDestino, int destinoId);

        /// <summary>
        /// Conjunto de DestinoId de un tipo ('Grupo' | 'Ingrediente') que TIENEN nota visible.
        /// Mismo candado que la lectura individual. Para sitemap y gates de indexado, sin N+1.
        /// </summary>
        Task<HashSet<int>> ObtenerDestinosConNotaVisibleAsync(string tipoDestino);
    }
}
