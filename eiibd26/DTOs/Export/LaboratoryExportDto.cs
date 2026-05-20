using System;

namespace eiibd26.DTOs.Export
{
    public sealed class LaboratoryExportDto
    {
        public string Estudio { get; init; } = "";
        public string Categoria { get; init; } = "";
        public string? Resultado { get; init; }
        public string? Unidad { get; init; }
        public DateTime? FechaResultado { get; init; }
        public string? Notas { get; init; }
        public string? Condicion { get; init; }
    }
}
