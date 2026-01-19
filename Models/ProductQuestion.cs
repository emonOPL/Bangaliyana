using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class ProductQuestion
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(1000)]
        [Display(Name = "Question")]
        public string Question { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Answer")]
        public string? Answer { get; set; }

        public string? AnsweredById { get; set; }

        [ForeignKey("AnsweredById")]
        public virtual ApplicationUser? AnsweredBy { get; set; }

        [Display(Name = "Is Answered")]
        public bool IsAnswered { get; set; } = false;

        [Display(Name = "Is Approved")]
        public bool IsApproved { get; set; } = false;

        [Display(Name = "Helpful Count")]
        public int HelpfulCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AnsweredAt { get; set; }
    }
}
