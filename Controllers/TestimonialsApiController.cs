using Bangaliyana.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Controllers
{
    [ApiController]
    [Route("api/testimonials")]
    public class TestimonialsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestimonialsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get positive testimonials (rating >= 4) for login page rotation
        /// </summary>
        [HttpGet("positive")]
        public async Task<IActionResult> GetPositiveTestimonials()
        {
            var testimonials = await _context.Testimonials
                .Where(t => t.IsActive && t.Rating >= 4)
                .OrderBy(t => Guid.NewGuid()) // Random order
                .Take(10) // Limit to 10 for performance
                .Select(t => new
                {
                    id = t.Id,
                    customerName = t.CustomerName,
                    customerTitle = t.CustomerTitle,
                    photoUrl = t.PhotoUrl,
                    content = t.Content,
                    rating = t.Rating
                })
                .ToListAsync();

            return Ok(testimonials);
        }

        /// <summary>
        /// Get all active testimonials
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTestimonials()
        {
            var testimonials = await _context.Testimonials
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.IsFeatured)
                .ThenBy(t => t.DisplayOrder)
                .Select(t => new
                {
                    id = t.Id,
                    customerName = t.CustomerName,
                    customerTitle = t.CustomerTitle,
                    customerLocation = t.CustomerLocation,
                    photoUrl = t.PhotoUrl,
                    content = t.Content,
                    rating = t.Rating,
                    isFeatured = t.IsFeatured
                })
                .ToListAsync();

            return Ok(testimonials);
        }

        /// <summary>
        /// Get positive platform reviews (rating >= 3) for login page rotation
        /// Uses both admin-added testimonials and user-submitted reviews
        /// </summary>
        [HttpGet("platform-reviews")]
        public async Task<IActionResult> GetPlatformReviews()
        {
            var reviews = await _context.Testimonials
                .Where(t => t.IsActive && t.IsApproved && t.Rating >= 3)
                .OrderBy(t => Guid.NewGuid()) // Random order
                .Take(20) // Limit for performance
                .Select(t => new
                {
                    id = t.Id,
                    customerName = t.CustomerName ?? "Anonymous",
                    customerTitle = t.IsVerifiedUser ? "Verified Customer" : (t.CustomerTitle ?? "Customer"),
                    photoUrl = t.PhotoUrl,
                    content = t.Content,
                    rating = t.Rating,
                    isVerified = t.IsVerifiedUser,
                    createdAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }
    }
}
