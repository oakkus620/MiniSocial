using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewProject.Data;
using NewProject.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            .Select(u => new {
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
        if (string.IsNullOrEmpty(username)) return Json(new List<object>());

        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        DateTime limit = DateTime.Now.AddHours(-24);

        var list = _context.Stories
            .Where(s => s.Username.ToLower() == username.ToLower() && s.DateCreated >= limit)
            .OrderBy(s => s.DateCreated) // Eskiden yeniye sıralı
            .ToList();

        var user = _context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
        string userPic = user?.ProfilePicturePath ?? "https://cdn-icons-png.flaticon.com/512/149/149071.png";

        var result = list.Select(s => new {
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
        string username = HttpContext.Session.GetString("GirisYapanKullanici")?? string.Empty;
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
    public IActionResult SendMessage(string receiverUsername, string content)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrWhiteSpace(content))
            return Json(new { success = false });

        // Bu sohbet daha önce sessize alınmış mı kontrol edelim
        var existingMessage = _context.Messages
            .Where(m => (m.SenderUsername == currentUsername && m.ReceiverUsername == receiverUsername) ||
                        (m.SenderUsername == receiverUsername && m.ReceiverUsername == currentUsername))
            .OrderByDescending(m => m.Date)
            .FirstOrDefault();

        // Eğer daha önce sessize alınmışsa, yeni mesajda da bu durumu koruyalım
        bool currentMuteState = existingMessage?.IsMuted ?? false;

        var msg = new MessageModels
        {
            SenderUsername = currentUsername,
            ReceiverUsername = receiverUsername,
            Content = content,
            Date = DateTime.Now,
            IsRead = false,
            IsMuted = currentMuteState // Önceki sessizlik durumu korunuyor!
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
            .Select(u => new {
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
    [HttpGet]
    public IActionResult GetChatMessages(string partnerUsername)
    {
        string currentUsername = HttpContext.Session.GetString("GirisYapanKullanici") ?? string.Empty;
        if (string.IsNullOrEmpty(currentUsername)) return Json(new { messages = new List<object>(), draft = "" });

        var messages = _context.Messages
            .Where(m => !m.IsDraft && ((m.SenderUsername == currentUsername && m.ReceiverUsername == partnerUsername) ||
                        (m.SenderUsername == partnerUsername && m.ReceiverUsername == currentUsername)))
            .OrderBy(m => m.Date)
            .Select(m => new {
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


}

