using System;
using System.ComponentModel.DataAnnotations;

public class MessageModels
{
    [Key]
    public int Id { get; set; }

    public string SenderUsername { get; set; } = string.Empty;
    public string ReceiverUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    // Okundu mu? (Görüldü durumu)
    public bool IsRead { get; set; } = false;

    // Sessize alındı mı? (Alıcı taraflı sessize alma durumu)
    public bool IsMuted { get; set; } = false;

    // Taslak mı? (Gönderilmemiş yarım kalan mesaj)
    public bool IsDraft { get; set;  } = false;

    public bool IsDeletedBySender { get; set; } = false;   // Gönderen kişi kendi ekranından sildiyse
    public bool IsDeletedByReceiver { get; set; } = false; // Alan kişi kendi ekranından sildiyse
    public bool IsDeletedForEveryone { get; set; } = false; // Her iki taraftan da silindiyse
}