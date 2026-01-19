using System.Text;

namespace Bangaliyana.Services
{
    public class ImageOptimizationService : IImageOptimizationService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageOptimizationService> _logger;
        private const string DEFAULT_IMAGE = "/images/noimage.jpg";

        // Standard responsive image widths
        private static readonly int[] StandardWidths = { 320, 480, 640, 800, 1024, 1280 };

        public ImageOptimizationService(IWebHostEnvironment environment, ILogger<ImageOptimizationService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public string GetOptimizedUrl(string? originalUrl, int? width = null, int? height = null, int quality = 80, string format = "webp")
        {
            if (string.IsNullOrEmpty(originalUrl))
                return DEFAULT_IMAGE;

            // Clean the URL
            var cleanUrl = originalUrl.TrimStart('~', '/');
            if (!cleanUrl.StartsWith("/"))
                cleanUrl = "/" + cleanUrl;

            // Build query parameters for image processing
            var queryParams = new List<string>();

            if (width.HasValue)
                queryParams.Add($"width={width.Value}");

            if (height.HasValue)
                queryParams.Add($"height={height.Value}");

            if (quality != 100)
                queryParams.Add($"quality={quality}");

            if (!string.IsNullOrEmpty(format) && format != "original")
                queryParams.Add($"format={format}");

            // If ImageSharp.Web is configured, these query params will be processed
            // Otherwise, the original image is returned
            if (queryParams.Any())
            {
                return cleanUrl + "?" + string.Join("&", queryParams);
            }

            return cleanUrl;
        }

        public string GetSrcSet(string? originalUrl, int[] widths, int quality = 80)
        {
            if (string.IsNullOrEmpty(originalUrl))
                return "";

            var srcsetItems = widths.Select(w =>
                $"{GetOptimizedUrl(originalUrl, width: w, quality: quality)} {w}w"
            );

            return string.Join(", ", srcsetItems);
        }

        public string GetThumbnailUrl(string? originalUrl, int size = 150)
        {
            return GetOptimizedUrl(originalUrl, width: size, height: size, quality: 75);
        }

        public string GetCardImageUrl(string? originalUrl)
        {
            // Standard card image size for product listings
            return GetOptimizedUrl(originalUrl, width: 300, quality: 80);
        }

        public string GetDetailImageUrl(string? originalUrl)
        {
            // Larger image for product detail pages
            return GetOptimizedUrl(originalUrl, width: 800, quality: 85);
        }

        public async Task<string> ProcessAndSaveImageAsync(Stream imageStream, string fileName, string folder)
        {
            try
            {
                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
                var relativePath = Path.Combine("images", folder, uniqueFileName);
                var absolutePath = Path.Combine(_environment.WebRootPath, relativePath);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Save the original file
                using (var fileStream = new FileStream(absolutePath, FileMode.Create))
                {
                    await imageStream.CopyToAsync(fileStream);
                }

                _logger.LogInformation("Image saved: {Path}", relativePath);

                return "/" + relativePath.Replace("\\", "/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process and save image: {FileName}", fileName);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> CreateImageVariantsAsync(string imagePath, string folder)
        {
            var variants = new Dictionary<string, string>();

            // For now, we use URL-based resizing via ImageSharp.Web
            // This method can be enhanced to pre-generate physical files if needed

            if (string.IsNullOrEmpty(imagePath))
                return variants;

            variants["thumbnail"] = GetThumbnailUrl(imagePath);
            variants["card"] = GetCardImageUrl(imagePath);
            variants["detail"] = GetDetailImageUrl(imagePath);
            variants["original"] = imagePath;

            // Generate srcset for responsive images
            variants["srcset"] = GetSrcSet(imagePath, StandardWidths);

            await Task.CompletedTask;
            return variants;
        }
    }

    /// <summary>
    /// HTML helper extensions for optimized images
    /// </summary>
    public static class ImageOptimizationExtensions
    {
        /// <summary>
        /// Generates an optimized img tag with lazy loading and responsive images support
        /// </summary>
        public static string OptimizedImage(
            string? src,
            string alt,
            string? cssClass = null,
            int? width = null,
            int? height = null,
            bool lazyLoad = true,
            bool responsive = false)
        {
            var imgTag = new StringBuilder("<img");

            // Add src
            var cleanSrc = string.IsNullOrEmpty(src) ? "/images/noimage.jpg" : src.TrimStart('~');
            if (width.HasValue)
            {
                cleanSrc += (cleanSrc.Contains("?") ? "&" : "?") + $"width={width}";
            }
            imgTag.Append($" src=\"{cleanSrc}\"");

            // Add responsive srcset
            if (responsive && !string.IsNullOrEmpty(src))
            {
                var srcset = GenerateSrcSet(src, new[] { 320, 480, 640, 800, 1024 });
                imgTag.Append($" srcset=\"{srcset}\"");
                imgTag.Append(" sizes=\"(max-width: 320px) 280px, (max-width: 480px) 440px, (max-width: 800px) 760px, 1024px\"");
            }

            // Add alt
            imgTag.Append($" alt=\"{System.Net.WebUtility.HtmlEncode(alt)}\"");

            // Add class
            if (!string.IsNullOrEmpty(cssClass))
            {
                imgTag.Append($" class=\"{cssClass}\"");
            }

            // Add dimensions
            if (width.HasValue)
                imgTag.Append($" width=\"{width}\"");
            if (height.HasValue)
                imgTag.Append($" height=\"{height}\"");

            // Add lazy loading
            if (lazyLoad)
            {
                imgTag.Append(" loading=\"lazy\"");
                imgTag.Append(" decoding=\"async\"");
            }

            imgTag.Append(" />");

            return imgTag.ToString();
        }

        private static string GenerateSrcSet(string src, int[] widths)
        {
            var cleanSrc = src.TrimStart('~', '/');
            if (!cleanSrc.StartsWith("/"))
                cleanSrc = "/" + cleanSrc;

            var items = widths.Select(w => $"{cleanSrc}?width={w} {w}w");
            return string.Join(", ", items);
        }
    }
}
