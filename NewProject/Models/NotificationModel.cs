using System;

public class NotificationModel
{
    public int Id { get; set; }

    // Bildirimi alacak olan kullanıcı (Kimin bildirimler sayfası?)
    public string OwnerUsername { get; set; }=string.Empty;

    // Bildirimi tetikleyen (eylemi yapan) kullanıcı
    public string SenderUsername { get; set; }=string.Empty;

    // Bildirim türü: "Like", "Comment", "Follow", "FollowRequest", "StoryLike", "StoryComment"
    public string Type { get; set; }=string.Empty;

    // İsteğe bağlı içerik veya gönderi ID'si (Tıklandığında ilgili gönderiye gitmek için)
    public int? TargetPostId { get; set; }
    public string Content { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}