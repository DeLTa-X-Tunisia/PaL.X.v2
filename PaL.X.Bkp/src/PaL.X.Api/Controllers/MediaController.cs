using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PaL.X.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MediaController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public MediaController(IWebHostEnvironment env)
        {
            _env = env;
        }

        private static readonly string[] AllowedVideoExtensions = new[] { ".mp4", ".mov", ".avi", ".webm" };
        private static readonly string[] AllowedAudioExtensions = new[] { ".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac" };
        private static readonly string[] AllowedImageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };
        private static readonly string[] AllowedDocumentExtensions = new[]
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".json", ".xml", ".zip", ".rar", ".7z"
        };

        private const long MaxUploadSizeBytes = 25 * 1024 * 1024; // 25 MB for generic uploads

        [HttpPost("upload")]
        [RequestSizeLimit(MaxUploadSizeBytes + (512 * 1024))]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes + (512 * 1024))]
        public async Task<IActionResult> Upload([FromForm] IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Aucun fichier reçu.");
            }

            if (file.Length > MaxUploadSizeBytes)
            {
                return BadRequest("Fichier trop volumineux (max 25 Mo).");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isVideo = AllowedVideoExtensions.Contains(extension);
            var isAudio = AllowedAudioExtensions.Contains(extension);
            var isImage = AllowedImageExtensions.Contains(extension);
            var isDoc = AllowedDocumentExtensions.Contains(extension);

            if (!isVideo && !isAudio && !isDoc && !isImage)
            {
                return BadRequest("Format non supporté. Vidéo: mp4/mov/avi/webm, Audio: mp3/wav/ogg/flac/m4a/aac, Image: png/jpg/jpeg/gif/webp, Docs: pdf/docx/xlsx/txt/zip…");
            }

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            if (!Directory.Exists(webRoot))
            {
                Directory.CreateDirectory(webRoot);
            }

            var folder = isVideo ? "videos" : isImage ? "images" : "files";
            var uploadsRoot = Path.Combine(webRoot, "uploads", folder);
            Directory.CreateDirectory(uploadsRoot);

            var safeName = $"{Guid.NewGuid():N}{extension}";
            var savePath = Path.Combine(uploadsRoot, safeName);

            await using (var stream = System.IO.File.Create(savePath))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var relativePath = $"/uploads/{folder}/{safeName}";
            var fullUrl = baseUrl + relativePath;

            return Ok(new
            {
                url = fullUrl,
                fileName = safeName,
                size = file.Length,
                contentType = file.ContentType,
                originalName = file.FileName,
                kind = isVideo ? "video" : (isAudio ? "audio" : (isImage ? "image" : "file"))
            });
        }
    }
}