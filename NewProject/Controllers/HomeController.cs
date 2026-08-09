using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewProject.Data;
using NewProject.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;



public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {

        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        bool hasUnread = _context.FollowRequests.Any(fr => fr.ReceiverUsername.ToLower() == currentUsername.ToLower() && fr.Status == "Pending");
        ViewBag.HasUnreadNotifications = hasUnread;


        if (string.IsNullOrEmpty(currentUsername))
        {
            return RedirectToAction("Login", "Account");
        }

        var followingUsernames = _context.Follows
            .Where(f => f.FollowerUsername == currentUsername)
            .Select(f => f.FollowingUsername)
            .ToList();

        followingUsernames.Add(currentUsername);

        var posts = _context.Posts
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Where(p => followingUsernames.Contains(p.Username))
                .OrderByDescending(p => p.Date)
                .ToList();

        var activeUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == currentUsername.ToLower());
        ViewBag.ActiveUser = activeUser;

        // --- HİKAYELER İÇİN AKTİF LİSTE ---
        DateTime expireLimit = DateTime.Now.AddHours(-24);
        var activeStories = _context.Stories
            .Where(s => s.DateCreated >= expireLimit && followingUsernames.Contains(s.Username))
            .OrderByDescending(s => s.DateCreated)
            .ToList();
        // Giriş yapan kullanıcının izlediği hikaye ID'lerini bulalım
        var seenStoryIds = _context.StoriesInteractions
            .Where(i => i.SenderUsername == currentUsername && i.Type == "seen")
            .Select(i => i.StoryId)
            .ToList();

        ViewBag.SeenStoryIds = seenStoryIds;

        ViewBag.ActiveStories = activeStories;
        // ---------------------------------

        return View(posts);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost(int? editPostId, string content, IFormFile? postMedia, string? existingMediaUrl)
    {
        string? username = HttpContext.Session.GetString("GirisYapanKullanici");
        if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

        if (editPostId.HasValue && editPostId.Value > 0)
        {
            var postToEdit = _context.Posts.FirstOrDefault(p => p.Id == editPostId.Value && p.Username == username);
            if (postToEdit != null)
            {
                postToEdit.Content = content ?? string.Empty;

                if (postMedia != null && postMedia.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(postMedia.FileName);
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/posts", fileName);

                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/posts"));

                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await postMedia.CopyToAsync(stream);
                    }

                    postToEdit.MediaUrl = "/uploads/posts/" + fileName;
                    string extension = Path.GetExtension(postMedia.FileName).ToLower();
                    postToEdit.MediaType = (extension == ".mp4" || extension == ".mov" || extension == ".avi" || extension == ".webm") ? "video" : "image";
                }
                else if (string.IsNullOrEmpty(existingMediaUrl))
                {
                    postToEdit.MediaUrl = null;
                    postToEdit.MediaType = null;
                }

                if (string.IsNullOrWhiteSpace(postToEdit.Content) && string.IsNullOrEmpty(postToEdit.MediaUrl))
                {
                    var relatedComments = _context.Comments.Where(c => c.PostId == postToEdit.Id);
                    var relatedLikes = _context.Likes.Where(l => l.PostId == postToEdit.Id);
                    _context.Comments.RemoveRange(relatedComments);
                    _context.Likes.RemoveRange(relatedLikes);
                    _context.Posts.Remove(postToEdit);
                }

                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        if (!string.IsNullOrEmpty(username) && (!string.IsNullOrWhiteSpace(content) || postMedia != null))
        {
            var newPost = new PostModels
            {
                Username = username,
                Content = content ?? string.Empty,
                Date = DateTime.Now
            };

            if (postMedia != null && postMedia.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(postMedia.FileName);
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/posts", fileName);

                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/posts"));

                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await postMedia.CopyToAsync(stream);
                }

                newPost.MediaUrl = "/uploads/posts/" + fileName;
                string extension = Path.GetExtension(postMedia.FileName).ToLower();
                newPost.MediaType = (extension == ".mp4" || extension == ".mov" || extension == ".avi" || extension == ".webm") ? "video" : "image";
            }

            _context.Posts.Add(newPost);
            _context.SaveChanges();
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult DeletePost(int id)
    {
        string? username = HttpContext.Session.GetString("GirisYapanKullanici");
        if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

        var post = _context.Posts
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .FirstOrDefault(p => p.Id == id);


        if (post != null && post.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
        {
            _context.Comments.RemoveRange(post.Comments);
            _context.Likes.RemoveRange(post.Likes);
            _context.Posts.Remove(post);
            _context.SaveChanges();
        }

        return RedirectToAction("Index");
    }


    [HttpPost]
    public IActionResult AddComment(int postId, string commentText)
    {
        string? username = HttpContext.Session.GetString("GirisYapanKullanici");
        if (string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(commentText))
            return Json(new { success = false });

        var user = _context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
        string userPic = !string.IsNullOrEmpty(user?.ProfilePicturePath) ? user.ProfilePicturePath : "https://cdn-icons-png.flaticon.com/512/149/149071.png";

        var comment = new CommentModel
        {
            PostId = postId,
            Username = username,
            UserProfilePicture = userPic,
            Content = commentText.Trim(),
            Date = DateTime.Now
        };

        _context.Comments.Add(comment);
        _context.SaveChanges();

        // Sayfayı yenilemek yerine yeni eklenen yorumun detaylarını JSON olarak dönüyoruz
        return Json(new
        {
            success = true,
            commentId = comment.Id,
            username = comment.Username,
            content = comment.Content,
            userProfilePicture = comment.UserProfilePicture,
            commentCount = _context.Comments.Count(c => c.PostId == postId)
        });
    }

    [HttpPost]
    public IActionResult EditComment(int commentId, string newContent)
    {
        string? username = HttpContext.Session.GetString("GirisYapanKullanici");
        var comment = _context.Comments.FirstOrDefault(c => c.Id == commentId && c.Username == username);

        if (comment != null && !string.IsNullOrWhiteSpace(newContent))
        {
            comment.Content = newContent.Trim();
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }


    [HttpPost] // Silme işlemini AJAX (POST) üzerinden yapıyoruz
    public IActionResult DeleteCommentAjax(int commentId)
    {
        string? username = HttpContext.Session.GetString("GirisYapanKullanici");
        if (string.IsNullOrEmpty(username)) return Json(new { success = false });

        var comment = _context.Comments.FirstOrDefault(c => c.Id == commentId && c.Username.ToLower() == username.ToLower());

        if (comment != null)
        {
            int postId = comment.PostId;
            _context.Comments.Remove(comment);
            _context.SaveChanges();

            int newCommentCount = _context.Comments.Count(c => c.PostId == postId);
            return Json(new { success = true, postId = postId, commentCount = newCommentCount });
        }
        return Json(new { success = false });
    }

    [HttpPost]
    public IActionResult ToggleLike(int postId)
    {
        string? username = HttpContext.Session.GetString("GirisYapanKullanici");
        if (string.IsNullOrEmpty(username)) return Json(new { success = false });

        var existingLike = _context.Likes.FirstOrDefault(l => l.PostId == postId && l.Username == username);
        bool isLiked;

        if (existingLike != null)
        {
            _context.Likes.Remove(existingLike);
            isLiked = false;
        }
        else
        {
            _context.Likes.Add(new LikeModel { PostId = postId, Username = username });
            isLiked = true;
        }
        _context.SaveChanges();

        int totalLikes = _context.Likes.Count(l => l.PostId == postId);
        return Json(new { success = true, isLiked = isLiked, count = totalLikes });
    }

    [HttpPost]
    public IActionResult ToggleFollow(string targetUsername)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Json(new { success = false });
        }

        if (string.Equals(currentUsername, targetUsername, StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { success = false });
        }

        var existingFollow = _context.Follows.FirstOrDefault(f =>
            f.FollowerUsername.ToLower() == currentUsername.ToLower() &&
            f.FollowingUsername.ToLower() == targetUsername.ToLower());

        bool isFollowingNow;

        if (existingFollow != null)
        {
            _context.Follows.Remove(existingFollow);
            isFollowingNow = false;
        }
        else
        {
            var newFollow = new Follow
            {
                FollowerUsername = currentUsername,
                FollowingUsername = targetUsername
            };
            _context.Follows.Add(newFollow);
            isFollowingNow = true;
        }

        _context.SaveChanges();

        int updatedFollowerCount = _context.Follows.Count(f => f.FollowingUsername.ToLower() == targetUsername.ToLower());

        return Json(new { success = true, isFollowing = isFollowingNow, followerCount = updatedFollowerCount });
    }

    [Route("Home/Profile/{username?}")]
    public IActionResult Profile(string username)
    {

        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        bool hasUnread = _context.FollowRequests.Any(fr => fr.ReceiverUsername.ToLower() == currentUsername.ToLower() && fr.Status == "Pending");
        ViewBag.HasUnreadNotifications = hasUnread;


        if (string.IsNullOrEmpty(currentUsername))
        {
            return RedirectToAction("Login", "Account");
        }

        // Eğer URL'den kullanıcı adı gelmediyse kendi profiline gidiyorsundur
        if (string.IsNullOrEmpty(username))
        {
            username = currentUsername;
        }

        // Kullanıcıyı veritabanından buluyoruz
        var profileUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
        if (profileUser == null) return NotFound();

        // Kullanıcının gönderileri
        var userPosts = _context.Posts
            .Where(p => p.Username.ToLower() == username.ToLower())
            .OrderByDescending(p => p.Date)
            .ToList();

        // Aktif kullanıcının (giriş yapanın) detayları (sol menü ve sağ üst için)
        var activeUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == currentUsername.ToLower());

        // Takipçi ve takip edilen sayıları
        int followersCount = _context.Follows.Count(f => f.FollowingUsername.ToLower() == username.ToLower());
        int followingCount = _context.Follows.Count(f => f.FollowerUsername.ToLower() == username.ToLower());
        bool isFollowing = _context.Follows.Any(f => f.FollowerUsername.ToLower() == currentUsername.ToLower() && f.FollowingUsername.ToLower() == username.ToLower());

        // Aktif hikayeler (Profil halkası için)
        DateTime expireLimit = DateTime.Now.AddHours(-24);
        var profileStoriesList = _context.Stories
            .Where(s => s.Username.ToLower() == username.ToLower() && s.DateCreated >= expireLimit)
            .OrderBy(s => s.DateCreated)
            .ToList();

        var seenStoryIds = _context.StoriesInteractions
            .Where(i => i.SenderUsername == currentUsername && i.Type == "seen")
            .Select(i => i.StoryId)
            .ToList();

        bool isMyProfile = profileUser.Username.ToLower() == currentUsername.ToLower();

        bool canSeePosts = isMyProfile || !profileUser.IsPrivate || isFollowing;

        userPosts = canSeePosts
           ? _context.Posts.Where(p => p.Username.ToLower() == username.ToLower()).OrderByDescending(p => p.Date).ToList()
           : new List<PostModels>();

        // ViewBag Değişkenleri (Arayüzün patlamaması için dolduruyoruz)
        ViewBag.ActiveUser = activeUser;
        ViewBag.ProfileUsername = profileUser.Username;
        ViewBag.ProfileUserBio = profileUser.Bio ?? "Biyografi yok";
        ViewBag.ProfileUserPic = !string.IsNullOrEmpty(profileUser.ProfilePicturePath) ? profileUser.ProfilePicturePath : "https://cdn-icons-png.flaticon.com/512/149/149071.png";
        ViewBag.PostCount = userPosts.Count;
        ViewBag.FollowersCount = followersCount;
        ViewBag.FollowingCount = followingCount;
        ViewBag.IsMyProfile = profileUser.Username.ToLower() == currentUsername.ToLower();
        ViewBag.IsFollowing = isFollowing;
        ViewBag.ProfileStoriesList = profileStoriesList;
        ViewBag.SeenStoryIds = seenStoryIds;
        ViewBag.CanSeePosts = canSeePosts;
        ViewBag.IsMyProfile = isMyProfile;
        ViewBag.ProfileUser = profileUser; // Hikaye halkası kontrolü için bunu ekledik!

        return View(userPosts);
    }

    [HttpGet]
    public IActionResult SearchUsers(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Json(new List<object>());
        }

        string searchKey = q.Trim().ToLower();

        var users = _context.Users
            .Where(u => u.Username.ToLower().Contains(searchKey))
            .Select(u => new
            {
                username = u.Username,
                profilePicture = !string.IsNullOrEmpty(u.ProfilePicturePath) ? u.ProfilePicturePath : "https://cdn-icons-png.flaticon.com/512/149/149071.png",
                bio = u.Bio ?? "Sosyal Medya Kullanıcısı"
            })
            .Take(10)
            .ToList();

        return Json(users);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStory(IFormFile storyMedia)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername))
        {
            return RedirectToAction("Login", "Account");
        }

        if (storyMedia != null && storyMedia.Length > 0)
        {
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(storyMedia.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await storyMedia.CopyToAsync(fileStream);
            }

            string mediaUrl = "/uploads/" + uniqueFileName;
            string mediaType = storyMedia.ContentType.StartsWith("video") ? "video" : "image";

            var story = new Story
            {
                Username = currentUsername,
                MediaUrl = mediaUrl,
                MediaType = mediaType,
                DateCreated = DateTime.Now // Doğru sütun adı kullanıldı
            };

            _context.Stories.Add(story);
            _context.SaveChanges();
        }

        string refererUrl = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(refererUrl))
        {
            return Redirect(refererUrl);
        }
        return RedirectToAction("Index");
    }


    [HttpGet]
    public IActionResult DeleteStory(int id)
    {
        string? username = HttpContext.Session.GetString("GirisYapanKullanici");
        var story = _context.Stories.FirstOrDefault(s => s.Id == id);

        if (story != null && story.Username.ToLower() == username?.ToLower())
        {
            _context.Stories.Remove(story);
            _context.SaveChanges();
        }

        string refererUrl = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(refererUrl))
        {
            return Redirect(refererUrl);
        }
        return RedirectToAction("Index");
    }

    // Çoklu Hikaye Listeleme Endpoint'i (Frontend oynatıcısı için gerekli)
    [HttpGet]
    public IActionResult GetUserStories(string username)
    {

        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;

        var targetUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
        bool isFollowing = _context.Follows.Any(f => f.FollowerUsername.ToLower() == currentUsername.ToLower() && f.FollowingUsername.ToLower() == username.ToLower());

        // Eğer hesap gizli ve takip etmiyorsan hikayeleri boş döndür!
        if (targetUser != null && targetUser.IsPrivate && !isFollowing && !string.Equals(currentUsername, username, StringComparison.OrdinalIgnoreCase))
        {
            return Json(new List<object>());
        }


        if (string.IsNullOrEmpty(username)) return Json(new List<object>());


        DateTime limit = DateTime.Now.AddHours(-24);

        var list = _context.Stories
            .Where(s => s.Username.ToLower() == username.ToLower() && s.DateCreated >= limit)
            .OrderBy(s => s.DateCreated) // Eskiden yeniye sıralı
            .ToList();

        var user = _context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
        string userPic = user?.ProfilePicturePath ?? "https://cdn-icons-png.flaticon.com/512/149/149071.png";

        var result = list.Select(s => new
        {
            id = s.Id,
            username = s.Username,
            mediaUrl = s.MediaUrl,
            mediaType = s.MediaType,
            date = s.DateCreated,
            userProfilePic = userPic,
            isLikedByMe = _context.StoriesInteractions.Any(i => i.StoryId == s.Id && i.SenderUsername == currentUsername && i.Type == "like"),
            isSeenByMe = _context.StoriesInteractions.Any(i => i.StoryId == s.Id && i.SenderUsername == currentUsername && i.Type == "seen")
        });

        return Json(result);
    }

    [HttpPost]
    public IActionResult InteractStory(int storyId, string type, string commentText = "")
    {
        string username = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(username))
        {
            return Json(new { success = false, message = "Oturum açılmamış." });
        }

        // Hikayenin sahibini bulmak gerekebilir (görüldü veya beğeni bildirimi için)
        var story = _context.Stories.FirstOrDefault(s => s.Id == storyId);
        string ownerUsername = story != null ? story.Username : string.Empty;

        if (type == "like")
        {
            // Kullanıcı bu hikayeyi daha önce beğenmiş mi?
            var existingLike = _context.StoriesInteractions
                .FirstOrDefault(i => i.StoryId == storyId && i.SenderUsername == username && i.Type == "like");

            bool isLikedNow = false;

            if (existingLike != null)
            {
                // Beğenilmişse kaldır (Toggle / Beğeniyi Geri Çek)
                _context.StoriesInteractions.Remove(existingLike);
                isLikedNow = false;
            }
            else
            {
                // Beğenilmemişse ekle
                var newLike = new StoryInteraction
                {
                    StoryId = storyId,
                    SenderUsername = username,
                    OwnerUsername = ownerUsername,
                    Type = "like",
                    Content = string.Empty,
                    Date = DateTime.Now
                };
                _context.StoriesInteractions.Add(newLike);
                isLikedNow = true;
            }

            _context.SaveChanges();
            return Json(new { success = true, isLiked = isLikedNow });
        }
        else if (type == "seen")
        {
            // Daha önce bu hikaye bu kullanıcı tarafından "seen" olarak kaydedilmiş mi?
            bool alreadySeen = _context.StoriesInteractions
                .Any(i => i.StoryId == storyId && i.SenderUsername == username && i.Type == "seen");

            if (!alreadySeen)
            {
                var seenRecord = new StoryInteraction
                {
                    StoryId = storyId,
                    SenderUsername = username,
                    OwnerUsername = ownerUsername,
                    Type = "seen",
                    Content = string.Empty,
                    Date = DateTime.Now
                };
                _context.StoriesInteractions.Add(seenRecord);
                _context.SaveChanges();
            }

            return Json(new { success = true });
        }
        else if (type == "comment")
        {
            var comment = new StoryInteraction
            {
                StoryId = storyId,
                SenderUsername = username,
                OwnerUsername = ownerUsername,
                Type = "comment",
                Content = commentText,
                Date = DateTime.Now
            };
            _context.StoriesInteractions.Add(comment);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        return Json(new { success = false });
    }


    [HttpPost]
    [HttpPost]
    public IActionResult SendMessage(string receiverUsername, string content)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrWhiteSpace(content))
            return Json(new { success = false });

        var existingMessage = _context.Messages
            .Where(m => (m.SenderUsername == currentUsername && m.ReceiverUsername == receiverUsername) ||
                        (m.SenderUsername == receiverUsername && m.ReceiverUsername == currentUsername))
            .OrderByDescending(m => m.Date)
            .FirstOrDefault();

        bool currentMuteState = existingMessage?.IsMuted ?? false;

        var msg = new MessageModels
        {
            SenderUsername = currentUsername,
            ReceiverUsername = receiverUsername,
            Content = content,
            Date = DateTime.Now,
            IsRead = false,
            IsMuted = currentMuteState
        };

        _context.Messages.Add(msg);
        _context.SaveChanges();

        return Json(new { success = true });
    }

    [HttpGet]
    public IActionResult GetConversations()
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return Json(new { conversations = new List<object>(), totalUnread = 0 });

        var allMessages = _context.Messages
            .Where(m => m.SenderUsername == currentUsername || m.ReceiverUsername == currentUsername)
            .OrderByDescending(m => m.Date)
            .ToList();

        var partners = allMessages
            .Select(m => m.SenderUsername == currentUsername ? m.ReceiverUsername : m.SenderUsername)
            .Distinct()
            .ToList();

        var conversationList = new List<object>();
        int totalUnreadCount = 0;

        foreach (var partner in partners)
        {
            var lastMsg = allMessages.FirstOrDefault(m => m.SenderUsername == partner || m.ReceiverUsername == partner);
            var partnerUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == partner.ToLower());

            // Alıcının kendisi olduğu ve okunmamış mesajlar
            bool isUnread = lastMsg != null && lastMsg.ReceiverUsername == currentUsername && !lastMsg.IsRead;
            if (isUnread) totalUnreadCount++;

            conversationList.Add(new
            {
                username = partner,
                profilePicture = partnerUser?.ProfilePicturePath ?? "https://cdn-icons-png.flaticon.com/512/149/149071.png",
                lastMessage = lastMsg != null ? lastMsg.Content : "",
                timeAgo = lastMsg != null ? GetTimeAgo(lastMsg.Date) : "",
                isRead = !isUnread,
                isMuted = lastMsg != null && lastMsg.IsMuted
            });
        }

        return Json(new { conversations = conversationList, totalUnread = totalUnreadCount });
    }

    [HttpPost]
    [HttpPost]
    public IActionResult UpdateMessageAction(string partnerUsername, string actionType)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return Json(new { success = false });

        var messages = _context.Messages
            .Where(m => (m.SenderUsername == currentUsername && m.ReceiverUsername == partnerUsername) ||
                        (m.SenderUsername == partnerUsername && m.ReceiverUsername == currentUsername))
            .ToList();

        if (actionType == "read")
        {
            foreach (var m in messages)
            {
                if (m.ReceiverUsername == currentUsername) m.IsRead = true;
            }
        }
        else if (actionType == "mute")
        {
            foreach (var m in messages) m.IsMuted = true;
        }
        else if (actionType == "unmute") // <-- BURASI EKLENECEK
        {
            foreach (var m in messages) m.IsMuted = false;
        }
        else if (actionType == "delete")
        {
            _context.Messages.RemoveRange(messages);
        }

        _context.SaveChanges();
        return Json(new { success = true });
    }

    private string GetTimeAgo(DateTime date)
    {
        var fark = DateTime.Now - date;
        return fark.TotalMinutes < 1 ? "Az önce" :
               fark.TotalHours < 1 ? $"{(int)fark.TotalMinutes}d" :
               fark.TotalDays < 1 ? $"{(int)fark.TotalHours}s" : $"{(int)fark.TotalDays}g";
    }

    [HttpGet]
    public IActionResult SearchAllUsers(string q)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(q)) return Json(new List<object>());

        var users = _context.Users
            .Where(u => u.Username.ToLower().Contains(q.ToLower()) && u.Username.ToLower() != currentUsername.ToLower())
            .Take(10)
            .Select(u => new
            {
                username = u.Username,
                profilePicture = !string.IsNullOrEmpty(u.ProfilePicturePath) ? u.ProfilePicturePath : "https://cdn-icons-png.flaticon.com/512/149/149071.png",
                bio = u.Bio
            })
            .ToList();

        return Json(users);
    }

    [HttpPost]
    public IActionResult SaveDraft(string receiverUsername, string content)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return Json(new { success = false });

        // Varsa eski taslakları temizle, yenisini kaydet
        var oldDrafts = _context.Messages.Where(m => m.SenderUsername == currentUsername && m.ReceiverUsername == receiverUsername && m.IsDraft);
        _context.Messages.RemoveRange(oldDrafts);

        if (!string.IsNullOrWhiteSpace(content))
        {
            var draft = new MessageModels
            {
                SenderUsername = currentUsername,
                ReceiverUsername = receiverUsername,
                Content = content,
                Date = DateTime.Now,
                IsDraft = true
            };
            _context.Messages.Add(draft);
        }
        _context.SaveChanges();
        return Json(new { success = true });
    }

    
    [HttpGet]
    public IActionResult GetChatMessages(string partnerUsername)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return Json(new { messages = new List<object>(), draft = "" });

        var messages = _context.Messages
            .Where(m => !m.IsDraft && ((m.SenderUsername == currentUsername && m.ReceiverUsername == partnerUsername) ||
                        (m.SenderUsername == partnerUsername && m.ReceiverUsername == currentUsername)))
            .OrderBy(m => m.Date)
            .Select(m => new
            {
                id = m.Id, // EKLENDİ: JavaScript'in mesajı silebilmesi için zorunlu!
                sender = m.SenderUsername,
                content = m.Content,
                date = m.Date.ToString("HH:mm")
            })
            .ToList();

        var draft = _context.Messages
            .FirstOrDefault(m => m.IsDraft && m.SenderUsername == currentUsername && m.ReceiverUsername == partnerUsername);

        return Json(new { messages, draft = draft?.Content ?? "" });
    }

    [HttpPost]
    public IActionResult MarkConversationAsRead(string partnerUsername)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return Json(new { success = false });

        var unreadMessages = _context.Messages
            .Where(m => m.ReceiverUsername == currentUsername && m.SenderUsername == partnerUsername && !m.IsRead)
            .ToList();

        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            _context.SaveChanges();
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [HttpPost]
    public IActionResult DeleteSingleMessage(int messageId, string deleteType)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return Json(new { success = false });

        var message = _context.Messages.FirstOrDefault(m => m.Id == messageId);
        if (message == null) return Json(new { success = false });

        if (deleteType == "self")
        {
            // "Benden sil" denildiğinde mesaj veritabanından komple siliniyor
            _context.Messages.Remove(message);
        }
        else if (deleteType == "everyone")
        {
            // "Herkesten sil" denildiğinde mesaj iki taraftan da komple kaldırılıyor
            _context.Messages.Remove(message);
        }

        _context.SaveChanges();
        return Json(new { success = true });
    }

    public IActionResult Messages()
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return RedirectToAction("Login", "Account");

        var activeUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == currentUsername.ToLower());
        ViewBag.ActiveUser = activeUser;
        return View();
    }



    // 1. Profil Sayfası Görüntüleme
    [HttpGet]
    [Route("Home/UserProfile/{targetUsername}")]
    public IActionResult UserProfile(string targetUsername)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return RedirectToAction("Login", "Account");

        if (string.Equals(currentUsername, targetUsername, StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Profile");

        var targetUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == targetUsername.ToLower());
        if (targetUser == null) return NotFound();

        // Takip ediyor muyuz kontrolü
        bool isFollowing = _context.Follows.Any(f =>
            f.FollowerUsername.ToLower() == currentUsername.ToLower() &&
            f.FollowingUsername.ToLower() == targetUsername.ToLower());

        // KRİTİK DÜZELTME: Hesap açık İSE VEYA takip ediyorsan gönderileri görebilirsin!
        bool canSeePosts = !targetUser.IsPrivate || isFollowing;

        var userPosts = canSeePosts
            ? _context.Posts.Where(p => p.Username.ToLower() == targetUsername.ToLower()).OrderByDescending(p => p.Date).ToList()
            : new List<PostModels>();

        var existingRequest = _context.FollowRequests.FirstOrDefault(f =>
            f.SenderUsername.ToLower() == currentUsername.ToLower() &&
            f.ReceiverUsername.ToLower() == targetUsername.ToLower() &&
            f.Status == "Pending");

        var incomingRequest = _context.FollowRequests.FirstOrDefault(f =>
            f.SenderUsername.ToLower() == targetUsername.ToLower() &&
            f.ReceiverUsername.ToLower() == currentUsername.ToLower() &&
            f.Status == "Pending");

        ViewBag.ActiveUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == currentUsername.ToLower());
        ViewBag.ProfileUser = targetUser;
        ViewBag.ProfileUsername = targetUser.Username;
        ViewBag.ProfileUserBio = targetUser.Bio;
        ViewBag.ProfileUserPic = targetUser.ProfilePicturePath ?? "https://cdn-icons-png.flaticon.com/512/149/149071.png";

        ViewBag.CanSeePosts = canSeePosts;
        ViewBag.IsFollowing = isFollowing;
        ViewBag.HasPendingRequest = existingRequest != null;
        ViewBag.HasIncomingRequest = incomingRequest != null;
        ViewBag.IncomingRequestId = incomingRequest?.Id;

        ViewBag.FollowersCount = _context.Follows.Count(f => f.FollowingUsername.ToLower() == targetUsername.ToLower());
        ViewBag.FollowingCount = _context.Follows.Count(f => f.FollowingUsername.ToLower() == targetUsername.ToLower());
        ViewBag.PostCount = _context.Posts.Count(p => p.Username.ToLower() == targetUsername.ToLower());

        return View(userPosts);
    }

    // 2. Takip Et / İstek Gönder / İptal Et Buton Mantığı
    [HttpPost]
    public IActionResult ToggleFollowRequest(string targetUsername)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return Json(new { success = false });

        if (string.Equals(currentUsername, targetUsername, StringComparison.OrdinalIgnoreCase))
            return Json(new { success = false });

        var targetUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == targetUsername.ToLower());
        if (targetUser == null) return Json(new { success = false });

        // Zaten takip ediyor mu?
        var existingFollow = _context.Follows.FirstOrDefault(f =>
            f.FollowerUsername.ToLower() == currentUsername.ToLower() &&
            f.FollowingUsername.ToLower() == targetUsername.ToLower());

        if (existingFollow != null)
        {
            // Takipten çık
            _context.Follows.Remove(existingFollow);
            _context.SaveChanges();
            return Json(new { success = true, status = "unfollowed" });
        }

        // Bekleyen istek var mı?
        var existingRequest = _context.FollowRequests.FirstOrDefault(f =>
            f.SenderUsername.ToLower() == currentUsername.ToLower() &&
            f.ReceiverUsername.ToLower() == targetUsername.ToLower() &&
            f.Status == "Pending");

        if (existingRequest != null)
        {
            // İsteği Geri Çek
            _context.FollowRequests.Remove(existingRequest);
            _context.SaveChanges();
            return Json(new { success = true, status = "cancelled" });
        }

        if (targetUser.IsPrivate)
        {
            // Hesap gizli -> İstek gönder
            var newRequest = new FollowRequest
            {
                SenderUsername = currentUsername,
                ReceiverUsername = targetUsername,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            _context.FollowRequests.Add(newRequest);
            _context.SaveChanges();
            return Json(new { success = true, status = "pending" });
        }
        else
        {
            // Hesap açık -> Doğrudan takibe başla
            var newFollow = new Follow
            {
                FollowerUsername = currentUsername,
                FollowingUsername = targetUsername
            };
            _context.Follows.Add(newFollow);
            _context.SaveChanges();
            return Json(new { success = true, status = "following" });
        }
    }

    // 3. Bildirimler Sayfası (Takip İsteklerini Listeleme)
    [HttpGet]
    public IActionResult Notifications()
    {

        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        bool hasUnread = _context.FollowRequests.Any(fr => fr.ReceiverUsername.ToLower() == currentUsername.ToLower() && fr.Status == "Pending");
        ViewBag.HasUnreadNotifications = hasUnread;


        if (string.IsNullOrEmpty(currentUsername)) return RedirectToAction("Login", "Account");

        // Bana gelen bekleyen (Pending) takip istekleri
        var pendingRequests = _context.FollowRequests
            .Where(fr => fr.ReceiverUsername.ToLower() == currentUsername.ToLower() && fr.Status == "Pending")
            .OrderByDescending(fr => fr.CreatedAt)
            .ToList();

        // İstek atan kullanıcıların profil fotoğraflarını vb. rahat çekebilmek için:
        var requestListWithDetails = new List<object>();
        foreach (var req in pendingRequests)
        {
            var senderUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == req.SenderUsername.ToLower());
            requestListWithDetails.Add(new
            {
                RequestId = req.Id,
                SenderUsername = req.SenderUsername,
                SenderProfilePic = senderUser?.ProfilePicturePath ?? "https://cdn-icons-png.flaticon.com/512/149/149071.png",
                CreatedAt = req.CreatedAt
            });
        }

        var activeUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == currentUsername.ToLower());
        ViewBag.ActiveUser = activeUser;
        ViewBag.PendingRequests = requestListWithDetails;

        return View();
    }

    // 4. Takip İsteğini Kabul Et veya Reddet
    [HttpPost]
    public IActionResult RespondFollowRequest(int requestId, string action) // action = "accept" veya "reject"
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return Json(new { success = false });

        var request = _context.FollowRequests.FirstOrDefault(fr => fr.Id == requestId && fr.ReceiverUsername.ToLower() == currentUsername.ToLower());
        if (request == null) return Json(new { success = false });

        if (action == "accept")
        {
            request.Status = "Accepted";

            // Takip tablosuna ekle (Artık birbirlerini takip ediyorlar)
            bool alreadyFollowing = _context.Follows.Any(f => f.FollowerUsername.ToLower() == request.SenderUsername.ToLower() && f.FollowingUsername.ToLower() == request.ReceiverUsername.ToLower());
            if (!alreadyFollowing)
            {
                _context.NotificationModels.Add(new NotificationModel
                {
                    OwnerUsername = request.SenderUsername,
                    SenderUsername = currentUsername,
                    Type = "AcceptRequest",
                    Content = $"{currentUsername} takip isteğini kabul etti.",
                    CreatedAt = DateTime.Now
                });
            }
        }
        else if (action == "reject")
        {
            // Reddedilirse isteği tamamen silebiliriz
            _context.FollowRequests.Remove(request);
        }

        _context.SaveChanges();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(string bio, IFormFile? profilePicture, bool isPrivate)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return RedirectToAction("Login", "Account");

        var user = _context.Users.FirstOrDefault(u => u.Username.ToLower() == currentUsername.ToLower());
        if (user != null)
        {
            user.Bio = bio;
            user.IsPrivate = isPrivate; // Gizlilik ayarı güncelleniyor

            if (profilePicture != null && profilePicture.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles", fileName);

                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles"));

                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                user.ProfilePicturePath = "/uploads/profiles/" + fileName;
            }

            _context.SaveChanges();
        }

        return RedirectToAction("Profile", new { username = currentUsername });
    }


    [HttpGet]
    public IActionResult GetFollowersList(string username)
    {
        var followerUsernames = _context.Follows
            .Where(f => f.FollowingUsername.ToLower() == username.ToLower())
            .Select(f => f.FollowerUsername)
            .ToList();

        var users = _context.Users
            .Where(u => followerUsernames.Contains(u.Username))
            .Select(u => new
            {
                username = u.Username,
                profilePicture = !string.IsNullOrEmpty(u.ProfilePicturePath) ? u.ProfilePicturePath : "https://cdn-icons-png.flaticon.com/512/149/149071.png",
                bio = u.Bio ?? "Sosyal Medya Kullanıcısı"
            })
            .ToList();

        return Json(users);
    }

    [HttpGet]
    public IActionResult GetFollowingList(string username)
    {
        var followingUsernames = _context.Follows
            .Where(f => f.FollowerUsername.ToLower() == username.ToLower())
            .Select(f => f.FollowingUsername)
            .ToList();

        var users = _context.Users
            .Where(u => followingUsernames.Contains(u.Username))
            .Select(u => new
            {
                username = u.Username,
                profilePicture = !string.IsNullOrEmpty(u.ProfilePicturePath) ? u.ProfilePicturePath : "https://cdn-icons-png.flaticon.com/512/149/149071.png",
                bio = u.Bio ?? "Sosyal Medya Kullanıcısı"
            })
            .ToList();

        return Json(users);
    }

    [HttpGet]
    public IActionResult GetPostDetails(int postId)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return Content("Oturum bulunamadı.");

        var post = _context.Posts
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .FirstOrDefault(p => p.Id == postId);

        if (post == null) return Content("Gönderi bulunamadı.");

        var postOwner = _context.Users.FirstOrDefault(u => u.Username.ToLower() == post.Username.ToLower());
        string ownerPic = (postOwner != null && !string.IsNullOrEmpty(postOwner.ProfilePicturePath)) ? postOwner.ProfilePicturePath : "https://cdn-icons-png.flaticon.com/512/149/149071.png";

        bool isLikedByMe = post.Likes.Any(l => l.Username == currentUsername);

        // Sol taraf medya, sağ taraf yorumlar/detaylar (Instagram Masaüstü Lightbox Tarzı)
        string mediaHtml = "";
        if (!string.IsNullOrEmpty(post.MediaUrl))
        {
            if (post.MediaType == "video")
            {
                mediaHtml = $"<video src='{post.MediaUrl}' controls autoplay muted loop style='width:100%; height:100%; object-fit:contain; background:#000;'></video>";
            }
            else
            {
                mediaHtml = $"<img src='{post.MediaUrl}' style='width:100%; height:100%; object-fit:contain; background:#000;' />";
            }
        }
        else
        {
            mediaHtml = $"<div style='display:flex; align-items:center; justify-content:center; width:100%; height:100%; padding:20px; text-align:center; color:#fff;'>{post.Content}</div>";
        }

        string commentsHtml = "";
        if (!post.Comments.Any())
        {
            commentsHtml = "<div style='color: #737373; font-size: 13px; text-align: center; margin-top: 40px;'>Henüz yorum yok.</div>";
        }
        else
        {
            foreach (var c in post.Comments)
            {
                commentsHtml += $@"
                <div style='display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 12px;'>
                    <div style='display: flex; gap: 10px;'>
                        <img src='{(string.IsNullOrEmpty(c.UserProfilePicture) ? "https://cdn-icons-png.flaticon.com/512/149/149071.png" : c.UserProfilePicture)}' style='width: 32px; height: 32px; border-radius: 50%; object-fit: cover;' />
                        <div>
                            <span style='font-weight: 600; font-size: 13px; color: #fff; margin-right: 6px;'>{c.Username}</span>
                            <span style='font-size: 13px; color: #fff;'>{c.Content}</span>
                        </div>
                    </div>
                </div>";
            }
        }

        string htmlResult = $@"
        <div style='display: flex; width: 100%; height: 600px; background: #000; color: #fff;'>
            <!-- SOL TARAF: MEDYA -->
            <div style='flex: 1.3; background: #000; display: flex; align-items: center; justify-content: center; overflow: hidden; border-right: 1px solid #262626;'>
                {mediaHtml}
            </div>
            <!-- SAĞ TARAF: BİLGİLER VE YORUMLAR -->
            <div style='flex: 1; display: flex; flex-direction: column; background: #000;'>
                <!-- Üst Bilgi -->
                <div style='display: flex; align-items: center; gap: 12px; padding: 14px 16px; border-bottom: 1px solid #262626;'>
                    <img src='{ownerPic}' style='width: 36px; height: 36px; border-radius: 50%; object-fit: cover;' />
                    <span style='font-weight: 600; font-size: 14px;'>{post.Username}</span>
                </div>
                <!-- Yorum Listesi -->
                <div style='flex: 1; overflow-y: auto; padding: 16px;'>
                    <div style='display: flex; gap: 10px; margin-bottom: 16px;'>
                        <img src='{ownerPic}' style='width: 36px; height: 36px; border-radius: 50%; object-fit: cover;' />
                        <div>
                            <span style='font-weight: 600; font-size: 13px; margin-right: 6px;'>{post.Username}</span>
                            <span style='font-size: 13px;'>{post.Content}</span>
                        </div>
                    </div>
                    {commentsHtml}
                </div>
                <!-- Alt İşlemler ve Yorum Yazma -->
                <div style='padding: 12px 16px; border-top: 1px solid #262626;'>
                    <div style='display: flex; gap: 16px; font-size: 22px; margin-bottom: 10px;'>
                        <i class='{(isLikedByMe ? "fa-solid fa-heart liked" : "fa-regular fa-heart")}'' style='cursor:pointer; color: {(isLikedByMe ? "#ed4956" : "#fff")}'></i>
                        <i class='fa-regular fa-comment' style='cursor:pointer;'></i>
                        <i class='fa-regular fa-paper-plane' style='cursor:pointer;'></i>
                    </div>
                    <div style='font-weight: 600; font-size: 13px; margin-bottom: 4px;'>{post.Likes.Count} beğeni</div>
                </div>
            </div>
        </div>";

        return Content(htmlResult, "text/html");
    }



    [HttpGet]
    public IActionResult GetLiveKitToken(string roomName, string participantIdentity)
    {
        // LiveKit panelinden aldığın bilgiler
        string apiKey = "APIPqXDPZgQhUE4";
        string apiSecret = "wss://newproje-3yt8yezy.livekit.cloud";

        // LiveKit Token Payload Yapısı
        var header = new { alg = "HS256", typ = "JWT" };

        long issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long expiresAt = issuedAt + 3600; // 1 saat geçerli

        var videoGrant = new Dictionary<string, object>
    {
        { "roomJoin", true },
        { "room", roomName },
        { "canPublish", true },
        { "canSubscribe", true }
    };

        var payload = new Dictionary<string, object>
    {
        { "iss", apiKey },
        { "sub", participantIdentity },
        { "nbf", issuedAt },
        { "exp", expiresAt },
        { "video", videoGrant }
    };

        string token = EncodeJwt(header, payload, apiSecret);

        return Json(new { token = token, wsUrl = "wss://newproje-3yt8yezy.livekit.cloud" });
    }

    // Güvenli JWT İmzalama Yardımcı Metodu
    private string EncodeJwt(object header, object payload, string secret)
    {
        string stringHeader = JsonSerializer.Serialize(header);
        string stringPayload = JsonSerializer.Serialize(payload);

        string base64Header = Convert.ToBase64String(Encoding.UTF8.GetBytes(stringHeader)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        string base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(stringPayload)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        string unsignedJwt = $"{base64Header}.{base64Payload}";

        byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
        byte[] messageBytes = Encoding.UTF8.GetBytes(unsignedJwt);

        string base64Signature;
        using (var hmac = new HMACSHA256(keyBytes))
        {
            byte[] hashMessage = hmac.ComputeHash(messageBytes);
            base64Signature = Convert.ToBase64String(hashMessage).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        return $"{unsignedJwt}.{base64Signature}";
    }
}

