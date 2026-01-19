using Bangaliyana.Data;
using Bangaliyana.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public class ProductBulkImportService : IProductBulkImportService
    {
        private readonly ApplicationDbContext _context;

        // Column headers for the template
        private static readonly string[] RequiredColumns = { "Name", "Description", "Price", "Stock" };
        private static readonly string[] AllColumns = {
            "Name", "Description", "Price", "DiscountPrice", "Stock", "SKU",
            "CategoryName", "Brand", "Fabric", "Weight", "CareInstructions",
            "ShortDescription", "Tags", "Status"
        };

        public ProductBulkImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public byte[] GenerateTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Products");

            // Add headers
            for (int i = 0; i < AllColumns.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = AllColumns[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // Mark required columns
            for (int i = 0; i < AllColumns.Length; i++)
            {
                if (RequiredColumns.Contains(AllColumns[i]))
                {
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                }
            }

            // Add instructions row
            var instructionRow = worksheet.Row(2);
            worksheet.Cell(2, 1).Value = "(Required) Product name";
            worksheet.Cell(2, 2).Value = "(Required) Product description";
            worksheet.Cell(2, 3).Value = "(Required) Price in BDT";
            worksheet.Cell(2, 4).Value = "(Optional) Sale price";
            worksheet.Cell(2, 5).Value = "(Required) Stock quantity";
            worksheet.Cell(2, 6).Value = "(Optional) Product SKU";
            worksheet.Cell(2, 7).Value = "(Optional) Category name - must match existing category";
            worksheet.Cell(2, 8).Value = "(Optional) Brand name";
            worksheet.Cell(2, 9).Value = "(Optional) Fabric/Material";
            worksheet.Cell(2, 10).Value = "(Optional) Weight in grams";
            worksheet.Cell(2, 11).Value = "(Optional) Care instructions";
            worksheet.Cell(2, 12).Value = "(Optional) Short description";
            worksheet.Cell(2, 13).Value = "(Optional) Tags (comma-separated)";
            worksheet.Cell(2, 14).Value = "(Optional) Draft/Active/Inactive";

            // Style instruction row
            for (int i = 1; i <= AllColumns.Length; i++)
            {
                worksheet.Cell(2, i).Style.Font.Italic = true;
                worksheet.Cell(2, i).Style.Font.FontColor = XLColor.Gray;
                worksheet.Cell(2, i).Style.Font.FontSize = 9;
            }

            // Add sample data row
            worksheet.Cell(3, 1).Value = "Sample Product Name";
            worksheet.Cell(3, 2).Value = "This is a sample product description";
            worksheet.Cell(3, 3).Value = 999;
            worksheet.Cell(3, 4).Value = 899;
            worksheet.Cell(3, 5).Value = 50;
            worksheet.Cell(3, 6).Value = "SKU-001";
            worksheet.Cell(3, 7).Value = "Clothing";
            worksheet.Cell(3, 8).Value = "Sample Brand";
            worksheet.Cell(3, 9).Value = "Cotton";
            worksheet.Cell(3, 10).Value = 250;
            worksheet.Cell(3, 11).Value = "Machine wash cold";
            worksheet.Cell(3, 12).Value = "Short desc here";
            worksheet.Cell(3, 13).Value = "tag1, tag2";
            worksheet.Cell(3, 14).Value = "Active";

            // Style sample row
            for (int i = 1; i <= AllColumns.Length; i++)
            {
                worksheet.Cell(3, i).Style.Font.FontColor = XLColor.Blue;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add notes sheet
            var notesSheet = workbook.Worksheets.Add("Instructions");
            notesSheet.Cell(1, 1).Value = "Bulk Product Import Instructions";
            notesSheet.Cell(1, 1).Style.Font.Bold = true;
            notesSheet.Cell(1, 1).Style.Font.FontSize = 14;

            notesSheet.Cell(3, 1).Value = "Required Fields (Green header):";
            notesSheet.Cell(3, 1).Style.Font.Bold = true;
            notesSheet.Cell(4, 1).Value = "- Name: Product name (max 200 characters)";
            notesSheet.Cell(5, 1).Value = "- Description: Full product description";
            notesSheet.Cell(6, 1).Value = "- Price: Regular price in BDT (must be > 0)";
            notesSheet.Cell(7, 1).Value = "- Stock: Available quantity (0 or more)";

            notesSheet.Cell(9, 1).Value = "Optional Fields:";
            notesSheet.Cell(9, 1).Style.Font.Bold = true;
            notesSheet.Cell(10, 1).Value = "- DiscountPrice: Sale price (must be less than Price)";
            notesSheet.Cell(11, 1).Value = "- CategoryName: Must match an existing category name exactly";
            notesSheet.Cell(12, 1).Value = "- Status: Draft, Active, Inactive (default: Active)";

            notesSheet.Cell(14, 1).Value = "Notes:";
            notesSheet.Cell(14, 1).Style.Font.Bold = true;
            notesSheet.Cell(15, 1).Value = "- Delete row 2 (instructions) and row 3 (sample) before uploading";
            notesSheet.Cell(16, 1).Value = "- Start your data from row 2 (after the header row)";
            notesSheet.Cell(17, 1).Value = "- Maximum 100 products per upload";
            notesSheet.Cell(18, 1).Value = "- Images must be added manually after import";

            notesSheet.Column(1).Width = 60;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public (bool IsValid, string ErrorMessage) ValidateFile(Stream fileStream)
        {
            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    return (false, "The Excel file contains no worksheets.");
                }

                // Check for required headers
                var headerRow = worksheet.Row(1);
                var headers = new List<string>();

                for (int col = 1; col <= AllColumns.Length; col++)
                {
                    var cellValue = headerRow.Cell(col).GetString().Trim();
                    if (!string.IsNullOrEmpty(cellValue))
                    {
                        headers.Add(cellValue);
                    }
                }

                foreach (var required in RequiredColumns)
                {
                    if (!headers.Contains(required, StringComparer.OrdinalIgnoreCase))
                    {
                        return (false, $"Required column '{required}' is missing from the file.");
                    }
                }

                // Check row count
                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                if (lastRow <= 1)
                {
                    return (false, "The file contains no data rows.");
                }

                if (lastRow > 102) // Header + instructions + sample + 100 data rows
                {
                    return (false, "Maximum 100 products can be imported at once.");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Invalid Excel file: {ex.Message}");
            }
        }

        public async Task<BulkImportResult> ImportProductsAsync(Stream fileStream, int sellerId)
        {
            var result = new BulkImportResult();

            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheets.First();

                // Get column indices
                var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var headerRow = worksheet.Row(1);
                for (int col = 1; col <= 20; col++)
                {
                    var header = headerRow.Cell(col).GetString().Trim();
                    if (!string.IsNullOrEmpty(header))
                    {
                        columnMap[header] = col;
                    }
                }

                // Get all categories for lookup
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToDictionaryAsync(c => c.Name.ToLower(), c => c.Id);

                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                result.TotalRows = lastRow - 1; // Exclude header

                // Process each row (skip header row 1)
                for (int row = 2; row <= lastRow; row++)
                {
                    var currentRow = worksheet.Row(row);

                    // Skip empty rows or instruction/sample rows
                    var nameValue = GetCellValue(currentRow, columnMap, "Name");
                    if (string.IsNullOrWhiteSpace(nameValue) ||
                        nameValue.StartsWith("(Required)") ||
                        nameValue.StartsWith("(Optional)") ||
                        nameValue == "Sample Product Name")
                    {
                        result.TotalRows--;
                        continue;
                    }

                    try
                    {
                        var product = new Products
                        {
                            SellerId = sellerId,
                            Name = nameValue,
                            Description = GetCellValue(currentRow, columnMap, "Description") ?? "",
                            ShortDescription = GetCellValue(currentRow, columnMap, "ShortDescription"),
                            Price = GetDecimalValue(currentRow, columnMap, "Price") ?? 0,
                            DiscountPrice = GetDecimalValue(currentRow, columnMap, "DiscountPrice"),
                            Stock = GetIntValue(currentRow, columnMap, "Stock") ?? 0,
                            SKU = GetCellValue(currentRow, columnMap, "SKU"),
                            Brand = GetCellValue(currentRow, columnMap, "Brand"),
                            Fabric = GetCellValue(currentRow, columnMap, "Fabric"),
                            Weight = GetDecimalValue(currentRow, columnMap, "Weight"),
                            CareInstructions = GetCellValue(currentRow, columnMap, "CareInstructions"),
                            Tags = GetCellValue(currentRow, columnMap, "Tags"),
                            ImageUrl = "/images/products/noimage.jpg",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        // Validation
                        var errors = ValidateProduct(product, row);
                        if (errors.Count > 0)
                        {
                            result.Errors.AddRange(errors);
                            result.FailedCount++;
                            continue;
                        }

                        // Set category
                        var categoryName = GetCellValue(currentRow, columnMap, "CategoryName");
                        if (!string.IsNullOrWhiteSpace(categoryName))
                        {
                            if (categories.TryGetValue(categoryName.ToLower(), out var categoryId))
                            {
                                product.CategoryId = categoryId;
                            }
                            else
                            {
                                result.Errors.Add($"Row {row}: Category '{categoryName}' not found. Product will be imported without category.");
                            }
                        }

                        // Set status
                        var statusStr = GetCellValue(currentRow, columnMap, "Status");
                        if (!string.IsNullOrWhiteSpace(statusStr))
                        {
                            if (Enum.TryParse<ProductStatus>(statusStr, true, out var status))
                            {
                                product.Status = status;
                            }
                        }

                        // Set availability based on stock and status
                        product.IsAvailable = product.Stock > 0 && product.Status == ProductStatus.Active;
                        if (product.Stock <= 0)
                        {
                            product.Status = ProductStatus.OutOfStock;
                        }

                        _context.Products.Add(product);
                        result.ImportedProducts.Add(product);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {row}: {ex.Message}");
                        result.FailedCount++;
                    }
                }

                // Save all imported products
                if (result.SuccessCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                result.Success = result.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Import failed: {ex.Message}");
            }

            return result;
        }

        private static string? GetCellValue(IXLRow row, Dictionary<string, int> columnMap, string columnName)
        {
            if (columnMap.TryGetValue(columnName, out var colIndex))
            {
                var value = row.Cell(colIndex).GetString().Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            return null;
        }

        private static decimal? GetDecimalValue(IXLRow row, Dictionary<string, int> columnMap, string columnName)
        {
            if (columnMap.TryGetValue(columnName, out var colIndex))
            {
                var cell = row.Cell(colIndex);
                if (cell.TryGetValue<decimal>(out var value))
                {
                    return value;
                }
                var strValue = cell.GetString().Trim();
                if (decimal.TryParse(strValue, out var parsed))
                {
                    return parsed;
                }
            }
            return null;
        }

        private static int? GetIntValue(IXLRow row, Dictionary<string, int> columnMap, string columnName)
        {
            if (columnMap.TryGetValue(columnName, out var colIndex))
            {
                var cell = row.Cell(colIndex);
                if (cell.TryGetValue<int>(out var value))
                {
                    return value;
                }
                var strValue = cell.GetString().Trim();
                if (int.TryParse(strValue, out var parsed))
                {
                    return parsed;
                }
            }
            return null;
        }

        private static List<string> ValidateProduct(Products product, int rowNumber)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                errors.Add($"Row {rowNumber}: Name is required.");
            }
            else if (product.Name.Length > 200)
            {
                errors.Add($"Row {rowNumber}: Name exceeds 200 characters.");
            }

            if (string.IsNullOrWhiteSpace(product.Description))
            {
                errors.Add($"Row {rowNumber}: Description is required.");
            }

            if (product.Price <= 0)
            {
                errors.Add($"Row {rowNumber}: Price must be greater than 0.");
            }

            if (product.DiscountPrice.HasValue && product.DiscountPrice >= product.Price)
            {
                errors.Add($"Row {rowNumber}: Discount price must be less than regular price.");
            }

            if (product.Stock < 0)
            {
                errors.Add($"Row {rowNumber}: Stock cannot be negative.");
            }

            return errors;
        }
    }
}
