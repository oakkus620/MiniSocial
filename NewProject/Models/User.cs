using System.ComponentModel.DataAnnotations;

namespace YourProject.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AdSoyad { get; set; }=string.Empty;

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

        // YENİ EKLENEN PROFİL ALANLARI:
        public string? Bio { get; set; }           // Biyografi yazısı
        public string? ProfilePicturePath { get; set; } // Fotoğrafın sunucudaki dosya yolu (örn: /uploads/profil.jpg)
    }
}