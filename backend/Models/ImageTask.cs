using System;

namespace Backend.Models
{
    public class ImageTask
    {
        public Guid Id { get; set; } 

        public int UserId { get; set; } 
        public User? User { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public ImageTaskStatus Status { get; set; } = ImageTaskStatus.Pending;
        public int Progress { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public string? ResultImageUrl { get; set; } 
    }
}