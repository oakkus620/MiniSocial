using System;
using System.ComponentModel.DataAnnotations;

public class StoryInteraction
{
    [Key]
    public int Id { get; set; }
    public int StoryId { get; set; }
    public string SenderUsername { get; set; } = string.Empty; // İşlemi yapan (yorum atan/beğenen)
    public string OwnerUsername { get; set; } = string.Empty; // Hikaye sahibi
    public string Type { get; set; } = string.Empty;         // "like" veya "comment"
    public string Content { get; set; }= string.Empty;       // Yorum metni (beğeni için boş olabilir)
    public DateTime Date { get; set; } = DateTime.Now;
}