using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.ShortUrl
{
    [Table("ShortUrlClicks")]
    public class ShortUrlClick
    {
        public long Id { get; set; }

        public int ShortUrlId { get; set; }

        public DateTime FechaClick { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ShortUrlId))]
        public ShortUrl? ShortUrl { get; set; }
    }
}
