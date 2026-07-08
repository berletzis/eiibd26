namespace eiibd26.Firma
{
    /// <summary>
    /// Firma de cobertura serializable: { "v":1, "totalTokens":N, "counts":{term→conteo} }.
    /// Formato compartido por Web (contenido propio) y Worker (externos).
    /// Los nombres JSON salen en camelCase por la política del serializador (v/totalTokens/counts);
    /// las CLAVES de counts (los términos) NO se transforman.
    /// </summary>
    public sealed class FirmaDto
    {
        public int V { get; set; }
        public int TotalTokens { get; set; }
        public Dictionary<string, int> Counts { get; set; } = new();
    }
}
