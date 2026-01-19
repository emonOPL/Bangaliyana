using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    public class Division
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<District> Districts { get; set; } = new List<District>();
    }
}