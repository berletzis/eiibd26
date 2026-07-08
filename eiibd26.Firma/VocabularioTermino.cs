using System.Text.RegularExpressions;

namespace eiibd26.Firma
{
    /// <summary>
    /// Término del vocabulario ya normalizado + su regex de frase precompilado
    /// (\b…\b sobre texto normalizado → cuenta la frase completa como unidad).
    /// </summary>
    public sealed class VocabularioTermino
    {
        public string Termino { get; }
        public Regex Patron { get; }

        public VocabularioTermino(string termino, Regex patron)
        {
            Termino = termino;
            Patron = patron;
        }
    }
}
