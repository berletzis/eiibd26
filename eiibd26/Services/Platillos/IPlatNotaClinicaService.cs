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
        /// <summary>La nota visible del destino, o null si no debe verse (candado cerrado o sin contenido).
        /// tipoNota ('Tolerancia' | 'Precaucion', Anexo 5) default 'Tolerancia' → los llamadores actuales
        /// siguen igual; la precaución (grupo de riesgo) pasa 'Precaucion'.</summary>
        Task<PlatNotaVisibleDto?> ObtenerNotaVisibleParaPacienteAsync(string tipoDestino, int destinoId, string tipoNota = "Tolerancia");

        /// <summary>
        /// Conjunto de DestinoId de un tipo ('Grupo' | 'Ingrediente') que TIENEN nota visible.
        /// Mismo candado que la lectura individual. Para sitemap y gates de indexado, sin N+1.
        /// </summary>
        Task<HashSet<int>> ObtenerDestinosConNotaVisibleAsync(string tipoDestino);

        /// <summary>
        /// Notas de alimentos sugeridas a un profesional para validar. MISMO candado que el
        /// resto del servicio: solo se sugiere lo que el profesional podrá validar de verdad
        /// (la tarjeta de validar únicamente aparece sobre notas visibles).
        ///
        /// Orden: primero las que este profesional NO ha validado, luego las que menos
        /// validaciones acumulan (más lo necesitan), luego por nombre.
        ///
        /// Vive aquí, y no en el servicio de validación, porque el candado de publicación es
        /// de este servicio y no debe duplicarse. Es lectura para el panel del profesional,
        /// no para el paciente.
        /// </summary>
        /// <param name="usuarioMedicoId">Id del profesional que consulta (para marcar lo suyo)</param>
        /// <param name="limite">Máximo de filas a devolver</param>
        Task<List<PlatNotaParaValidarDto>> ObtenerNotasParaValidarAsync(
            string usuarioMedicoId,
            int limite = 10,
            CancellationToken cancellationToken = default);
    }
}
