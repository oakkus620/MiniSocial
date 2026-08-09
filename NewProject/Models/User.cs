using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YourProject.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AdSoyad { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public int BirthDay { get; set; }
        public int BirthMonth { get; set; }
        public int BirthYear { get; set; }

        public bool IsEmailConfirmed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Profil Alanları:
        public string? Bio { get; set; }
        public string? ProfilePicturePath { get; set; }

        // GİZLİ HESAP ÖZELLİĞİ:
        public bool IsPrivate { get; set; } = false; // Varsayılan olarak hesap açık (public) başlar
    }
}