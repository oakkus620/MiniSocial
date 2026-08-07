using System.ComponentModel.DataAnnotations;

public class Follow
{
    [Key]
    public int Id { get; set; }

    // Takip eden kullanıcı adı
    public string FollowerUsername { get; set; } = string.Empty;

    // Takip edilen kullanıcı adı
    public string FollowingUsername { get; set; }= string.Empty;
}