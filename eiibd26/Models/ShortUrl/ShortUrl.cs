using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.ShortUrl
{
    [Table("ShortUrls")]
    public class ShortUrl
    {
        public int Id { get; set; }

        [Required, MaxLength(16)]
        public string Codigo { get; set; } = "";

        [Required, MaxLength(2000)]
        public string UrlDestino { get; set; } = "";

        public int ClickCount { get; set; } = 0;

        [MaxLength(50)]
        public string? Origen { get; set; }

        public DateTime FechaCreado { get; set; } = DateTime.UtcNow;

        public bool Eliminado { get; set; } = false;
    }
}
