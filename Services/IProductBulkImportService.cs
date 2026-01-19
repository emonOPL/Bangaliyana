using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public class BulkImportResult
    {
        public bool Success { get; set; }
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<Products> ImportedProducts { get; set; } = new List<Products>();
    }

    public interface IProductBulkImportService
    {
        /// <summary>
        /// Generates an Excel template for bulk product import
        /// </summary>
        byte[] GenerateTemplate();

        /// <summary>
        /// Imports products from an Excel file
        /// </summary>
        Task<BulkImportResult> ImportProductsAsync(Stream fileStream, int sellerId);

        /// <summary>
        /// Validates the Excel file structure
        /// </summary>
        (bool IsValid, string ErrorMessage) ValidateFile(Stream fileStream);
    }
}
