using Microsoft.EntityFrameworkCore;
using NewProject.Models;
using YourProject.Models;

namespace NewProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> db) : base(db)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<PostModels> Posts { get; set; }
        public DbSet<LikeModel> Likes { get; set; }
        public DbSet<CommentModel> Comments { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<StoryInteraction> StoriesInteractions { get; set; }
        public DbSet<MessageModels> Messages { get; set; }

        public DbSet<FollowRequest> FollowRequests { get; set; }

        public DbSet<NotificationModel> NotificationModels { get; set; }


       


        // SQL'deki tablo isimleriyle C# sınıflarını birebir eşcinselliyoruz / eşitliyoruz
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LikeModel>().ToTable("LikeModel");
            modelBuilder.Entity<CommentModel>().ToTable("CommentModel");


            // Takip isteği ilişkileri için çakışma (cascade delete) hatasını önlemek adına:
            modelBuilder.Entity<LikeModel>().ToTable("LikeModel");
            modelBuilder.Entity<CommentModel>().ToTable("CommentModel");
        }
    }
}