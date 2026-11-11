using System;

namespace Backend.Models
{
    public class ImageTask
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public string? OriginalFileName { get; set; }
        public ImageTaskStatus Status { get; set; }
        public int Progress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string? ResultImageUrl { get; set; }

        public int ServerPort { get; set; }
    }
}