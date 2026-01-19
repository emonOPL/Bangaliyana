namespace Bangaliyana.Services
{
    public interface IImageOptimizationService
    {
        /// <summary>
        /// Gets the optimized image URL with query parameters for resizing and format conversion
        /// </summary>
        /// <param name="originalUrl">Original image URL</param>
        /// <param name="width">Target width (null for original)</param>
        /// <param name="height">Target height (null for original)</param>
        /// <param name="quality">Image quality 1-100 (default 80)</param>
        /// <param name="format">Output format (webp, jpg, png - default webp)</param>
        /// <returns>Optimized image URL</returns>
        string GetOptimizedUrl(string? originalUrl, int? width = null, int? height = null, int quality = 80, string format = "webp");

        /// <summary>
        /// Gets srcset attribute for responsive images
        /// </summary>
        /// <param name="originalUrl">Original image URL</param>
        /// <param name="widths">Array of widths for srcset</param>
        /// <param name="quality">Image quality</param>
        /// <returns>srcset attribute value</returns>
        string GetSrcSet(string? originalUrl, int[] widths, int quality = 80);

        /// <summary>
        /// Gets the thumbnail URL for a product image
        /// </summary>
        string GetThumbnailUrl(string? originalUrl, int size = 150);

        /// <summary>
        /// Gets the card size image URL for product listings
        /// </summary>
        string GetCardImageUrl(string? originalUrl);

        /// <summary>
        /// Gets the detail page image URL
        /// </summary>
        string GetDetailImageUrl(string? originalUrl);

        /// <summary>
        /// Processes and saves an uploaded image with optimization
        /// </summary>
        /// <param name="imageStream">The uploaded image stream</param>
        /// <param name="fileName">Original filename</param>
        /// <param name="folder">Target folder (e.g., "products", "banners")</param>
        /// <returns>The saved image relative path</returns>
        Task<string> ProcessAndSaveImageAsync(Stream imageStream, string fileName, string folder);

        /// <summary>
        /// Creates multiple size variants of an image
        /// </summary>
        Task<Dictionary<string, string>> CreateImageVariantsAsync(string imagePath, string folder);
    }
}
