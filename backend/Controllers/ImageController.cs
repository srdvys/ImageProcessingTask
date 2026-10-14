using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ImageProcessingService _processingService;

        public ImageController(AppDbContext context, ImageProcessingService processingService)
        {
            _context = context;
            _processingService = processingService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Будь ласка, виберіть файл.");

            var user = _context.Users.FirstOrDefault();
            if (user == null)
            {
                user = new User { Username = "TestUser", PasswordHash = "TestHash123" };
                _context.Users.Add(user);
                await _context.SaveChangesAsync(); 
            }

            var imageTask = new ImageTask
            {
                Id = Guid.NewGuid(),
                UserId = user.Id, 
                OriginalFileName = file.FileName,
                Status = ImageTaskStatus.Processing,
                Progress = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.ImageTasks.Add(imageTask);
            await _context.SaveChangesAsync();

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var inputPath = Path.Combine(uploadsFolder, $"{imageTask.Id}_original.jpg");
            var outputPath = Path.Combine(uploadsFolder, $"{imageTask.Id}_processed.jpg");

            using (var stream = new FileStream(inputPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _processingService.ApplyHeavyFilter(inputPath, outputPath, progress =>
            {
                Console.WriteLine($"Прогрес обробки {imageTask.Id}: {progress}%");
            });

            imageTask.Status = ImageTaskStatus.Completed;
            imageTask.Progress = 100;
            imageTask.CompletedAt = DateTime.UtcNow;
            imageTask.ResultImageUrl = $"/images/{imageTask.Id}_processed.jpg";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Зображення успішно оброблено!",
                TaskId = imageTask.Id,
                ResultUrl = imageTask.ResultImageUrl
            });
        }
    }
}