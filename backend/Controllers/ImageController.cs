using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ImageProcessingService _processingService;
        private readonly IWebHostEnvironment _env;

        private static readonly ConcurrentDictionary<string, int> _liveProgress = new ConcurrentDictionary<string, int>();
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _cancelTokens = new ConcurrentDictionary<string, CancellationTokenSource>();

        public ImageController(AppDbContext context, ImageProcessingService processingService, IWebHostEnvironment env)
        {
            _context = context;
            _processingService = processingService;
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[УВАГА] ЗАПИТ ОБРОБЛЯЄ СЕРВЕР НА ПОРТУ: {HttpContext.Connection.LocalPort}");
            Console.ResetColor();

            var file = Request.Form.Files.FirstOrDefault();
            var filterType = Request.Form["filterType"].ToString();
            var taskId = Request.Form["taskId"].ToString();

            if (file == null || file.Length == 0) return BadRequest("Будь ласка, виберіть файл.");
            if (file.Length > 5 * 1024 * 1024) return BadRequest("Перевищено ліміт трудомісткості: розмір файлу більший за 5 МБ.");
            if (string.IsNullOrEmpty(taskId)) taskId = Guid.NewGuid().ToString();

            var user = _context.Users.FirstOrDefault();
            if (user == null)
            {
                user = new User { Username = "TestUser", PasswordHash = "TestHash123" };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var imageTask = new ImageTask
            {
                Id = Guid.Parse(taskId),
                UserId = user.Id,
                OriginalFileName = file.FileName,
                Status = ImageTaskStatus.Processing,
                Progress = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.ImageTasks.Add(imageTask);
            await _context.SaveChangesAsync();

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "images");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var inputPath = Path.Combine(uploadsFolder, $"{imageTask.Id}_original.jpg");
            var outputPath = Path.Combine(uploadsFolder, $"{imageTask.Id}_processed.jpg");

            using (var stream = new FileStream(inputPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _liveProgress[taskId] = 0;

            var cts = new CancellationTokenSource();
            _cancelTokens[taskId] = cts;

            try
            {
                _processingService.ApplyHeavyFilter(inputPath, outputPath, progress =>
                {
                    _liveProgress[taskId] = progress;
                }, filterType, cts.Token);

                imageTask.Status = ImageTaskStatus.Completed;
                imageTask.Progress = 100;
                imageTask.CompletedAt = DateTime.UtcNow;
                imageTask.ResultImageUrl = $"/images/{imageTask.Id}_processed.jpg";
            }
            catch (OperationCanceledException)
            {
                imageTask.Status = (ImageTaskStatus)99;
                imageTask.Progress = -1;
                imageTask.CompletedAt = DateTime.UtcNow;
            }
            finally
            {
                await _context.SaveChangesAsync();
                _liveProgress.TryRemove(taskId, out _);
                _cancelTokens.TryRemove(taskId, out _);
            }

            return Ok(new { Message = "Обробку завершено або зупинено", TaskId = imageTask.Id });
        }

        [HttpPost("cancel/{id}")]
        public IActionResult CancelTask(string id)
        {
            if (_cancelTokens.TryGetValue(id, out var cts))
            {
                cts.Cancel();
                return Ok(new { message = "Надіслано команду на скасування." });
            }
            return NotFound("Завдання не знайдено або вже завершено.");
        }

        [HttpGet("status/{id}")]
        public IActionResult GetTaskStatus(string id)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[ІНФО] Перевірка прогресу на порту: {HttpContext.Connection.LocalPort}");
            Console.ResetColor();

            if (_liveProgress.TryGetValue(id, out int liveProgress))
                return Ok(new { progress = liveProgress, status = "Processing" });

            if (Guid.TryParse(id, out Guid guidId))
            {
                var task = _context.ImageTasks.Find(guidId);
                if (task != null)
                {
                    if (task.Progress == -1) return Ok(new { progress = 0, status = "Cancelled", resultUrl = "" });

                    return Ok(new { progress = task.Progress, status = task.Status.ToString(), resultUrl = task.ResultImageUrl });
                }
            }
            return NotFound();
        }
    }
}