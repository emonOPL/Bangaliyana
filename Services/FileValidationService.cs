using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Bangaliyana.Services
{
    public interface IFileValidationService
    {
        /// <summary>
        /// Validates an image file (jpg, jpeg, png only)
        /// </summary>
        bool ValidateImage(IFormFile file, int maxSizeMB, out string error);

        /// <summary>
        /// Validates a document file (jpg, jpeg, png, pdf)
        /// </summary>
        bool ValidateDocument(IFormFile file, int maxSizeMB, out string error);

        /// <summary>
        /// Validates file content by checking magic bytes (file signature)
        /// </summary>
        bool ValidateFileSignature(IFormFile file, out string error);

        /// <summary>
        /// Saves a file to the specified folder with a unique name
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, string folder, string prefix = "");

        /// <summary>
        /// Deletes a file by its URL path
        /// </summary>
        void DeleteFile(string? fileUrl);

        /// <summary>
        /// Gets the full path for a file URL
        /// </summary>
        string GetFilePath(string fileUrl);
    }

    public class FileValidationService : IFileValidationService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FileValidationService> _logger;
        private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png" };
        private readonly string[] _documentExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };

        public FileValidationService(IWebHostEnvironment webHostEnvironment, ILogger<FileValidationService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public bool ValidateImage(IFormFile file, int maxSizeMB, out string error)
        {
            error = string.Empty;

            if (file == null || file.Length == 0)
            {
                error = "Please select a file to upload.";
                return false;
            }

            // Check file size
            var maxSizeBytes = maxSizeMB * 1024 * 1024;
            if (file.Length > maxSizeBytes)
            {
                error = $"File size must be less than {maxSizeMB}MB. Current size: {Math.Round(file.Length / (1024.0 * 1024.0), 2)}MB";
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_imageExtensions.Contains(extension))
            {
                error = $"Only image files are allowed ({string.Join(", ", _imageExtensions)}). Received: {extension}";
                return false;
            }

            // Check MIME type
            var validMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            if (!validMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                error = "Invalid file type. Please upload a valid image file (JPG, JPEG, or PNG).";
                return false;
            }

            return true;
        }

        public bool ValidateDocument(IFormFile file, int maxSizeMB, out string error)
        {
            error = string.Empty;

            if (file == null || file.Length == 0)
            {
                error = "Please select a file to upload.";
                return false;
            }

            // Check file size
            var maxSizeBytes = maxSizeMB * 1024 * 1024;
            if (file.Length > maxSizeBytes)
            {
                error = $"File size must be less than {maxSizeMB}MB. Current size: {Math.Round(file.Length / (1024.0 * 1024.0), 2)}MB";
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_documentExtensions.Contains(extension))
            {
                error = $"Only document files are allowed ({string.Join(", ", _documentExtensions)}). Received: {extension}";
                return false;
            }

            // Check MIME type
            var validMimeTypes = new[] { "image/jpeg", "image/png", "image/jpg", "application/pdf" };
            if (!validMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                error = "Invalid file type. Please upload a valid file (JPG, JPEG, PNG, or PDF).";
                return false;
            }

            return true;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folder, string prefix = "")
        {
            // Sanitize folder name - remove path traversal characters and invalid chars
            var sanitizedFolder = SanitizeFolderName(folder);
            var sanitizedPrefix = SanitizeFileName(prefix);

            // Create directory if it doesn't exist
            var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", sanitizedFolder);

            // Validate path is still under webroot
            var webRootFullPath = Path.GetFullPath(_webHostEnvironment.WebRootPath);
            var resolvedUploadPath = Path.GetFullPath(uploadPath);
            if (!resolvedUploadPath.StartsWith(webRootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid upload folder specified.");
            }

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate unique filename
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{sanitizedPrefix}{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the URL path
            return $"/images/{sanitizedFolder}/{fileName}";
        }

        /// <summary>
        /// Validates file content by checking magic bytes (file signature)
        /// </summary>
        public bool ValidateFileSignature(IFormFile file, out string error)
        {
            error = string.Empty;

            if (file == null || file.Length < 4)
            {
                error = "Invalid file.";
                return false;
            }

            using var stream = file.OpenReadStream();
            var header = new byte[8];
            stream.Read(header, 0, Math.Min(8, (int)file.Length));

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Validate magic bytes based on extension
            bool isValid = extension switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
                _ => false
            };

            if (!isValid)
            {
                error = "File content does not match the declared file type.";
            }

            return isValid;
        }

        private static string SanitizeFolderName(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return "uploads";

            // Remove path traversal characters and invalid chars
            var sanitized = folder
                .Replace("..", string.Empty)
                .Replace("/", string.Empty)
                .Replace("\\", string.Empty);

            foreach (var c in Path.GetInvalidPathChars())
            {
                sanitized = sanitized.Replace(c.ToString(), string.Empty);
            }

            return string.IsNullOrEmpty(sanitized) ? "uploads" : sanitized;
        }

        private static string SanitizeFileName(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return string.Empty;

            var sanitized = prefix;
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(c.ToString(), string.Empty);
            }

            return sanitized;
        }

        public void DeleteFile(string? fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            try
            {
                var filePath = GetFilePath(fileUrl);
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (IOException ex)
            {
                // Log file deletion errors - file might be in use or locked
                _logger.LogWarning(ex, "Failed to delete file: {FileUrl}", fileUrl);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Log permission errors
                _logger.LogWarning(ex, "Permission denied when deleting file: {FileUrl}", fileUrl);
            }
        }

        public string GetFilePath(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return string.Empty;

            // Remove leading slash and normalize path separators
            var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

            // Prevent path traversal attacks - remove any ".." sequences
            relativePath = relativePath.Replace("..", string.Empty);

            // Remove any potentially dangerous characters
            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in invalidChars)
            {
                relativePath = relativePath.Replace(c.ToString(), string.Empty);
            }

            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

            // Ensure the resulting path is still under webroot (prevents path traversal)
            var webRootFullPath = Path.GetFullPath(_webHostEnvironment.WebRootPath);
            var resolvedPath = Path.GetFullPath(fullPath);

            if (!resolvedPath.StartsWith(webRootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                // Path traversal attempt detected - return empty string
                return string.Empty;
            }

            return resolvedPath;
        }
    }
}
