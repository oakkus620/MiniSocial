using System.ComponentModel.DataAnnotations;

namespace NewProject.Models
{
    public class Story
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; }=string.Empty;
        public string MediaUrl { get; set; }=string.Empty;
        public string MediaType { get; set; }=string.Empty;
        public DateTime DateCreated { get; set; }

    }
}
