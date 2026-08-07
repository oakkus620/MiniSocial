using System.ComponentModel.DataAnnotations;

public class PostModels
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; } // "image" veya "video"

    public DateTime Date { get; set; } = DateTime.Now;

    // İlişkiler
    public virtual ICollection<CommentModel> Comments { get; set; } = new List<CommentModel>();
    public virtual ICollection<LikeModel> Likes { get; set; } = new List<LikeModel>();
}

public class CommentModel
{
    [Key]
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string UserProfilePicture { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public virtual PostModels Post { get; set; } = null!;
}

public class LikeModel
{
    [Key]
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Username { get; set; } = string.Empty;

    public virtual PostModels Post { get; set; } = null!;
}