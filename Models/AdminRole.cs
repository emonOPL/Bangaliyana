using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class AdminRole
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Role Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is System Role")]
        public bool IsSystemRole { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<AdminPermission> Permissions { get; set; } = new List<AdminPermission>();
        public virtual ICollection<AdminRoleAssignment> Assignments { get; set; } = new List<AdminRoleAssignment>();
    }

    public class AdminPermission
    {
        public int Id { get; set; }

        [Required]
        public int AdminRoleId { get; set; }

        [ForeignKey("AdminRoleId")]
        public virtual AdminRole? AdminRole { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Module")]
        public string Module { get; set; } = string.Empty;

        [Display(Name = "Can View")]
        public bool CanView { get; set; } = false;

        [Display(Name = "Can Create")]
        public bool CanCreate { get; set; } = false;

        [Display(Name = "Can Edit")]
        public bool CanEdit { get; set; } = false;

        [Display(Name = "Can Delete")]
        public bool CanDelete { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AdminRoleAssignment
    {
        public int Id { get; set; }

        [Required]
        public int AdminRoleId { get; set; }

        [ForeignKey("AdminRoleId")]
        public virtual AdminRole? AdminRole { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public string? AssignedBy { get; set; }
    }
}
