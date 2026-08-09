using System;

public class FollowRequest
{
    public int Id { get; set; }

    // İsteği gönderen kullanıcının adı
    public string SenderUsername { get; set; }

    // İsteğin gönderildiği (gizli hesap sahibi) kullanıcının adı
    public string ReceiverUsername { get; set; }

    // İstek durumu: "Pending", "Accepted", "Rejected"
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}