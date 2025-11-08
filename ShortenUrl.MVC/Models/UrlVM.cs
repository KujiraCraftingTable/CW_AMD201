using Microsoft.AspNetCore.Mvc;
using shortenUrl.MVC.commons;
using System.ComponentModel.DataAnnotations;

namespace shortenUrl.MVC.Models
{
    [Bind("Id,OriginalUrl,ShortenedUrl")]
    public class UrlVM
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Original URL")]
        public string OriginalUrl { get; set; } = string.Empty;

        [MaxLength(MaxLenghts.ShortenedUrl)]
        [Display(Name = "Custom Short URL")]
        public string ShortenedUrl { get; set; } = string.Empty;
    }
}

