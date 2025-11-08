using shortenUrl.MVC.commons;
using System.ComponentModel.DataAnnotations;

namespace shortenUrl.MVC.Data.Entities
{
    public class Url
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OriginalUrl { get; set; } = string.Empty;

        [MaxLength(MaxLenghts.ShortenedUrl)]
        public string ShortenedUrl { get; set; } = string.Empty;
    }
}
