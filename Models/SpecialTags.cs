﻿using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    public class SpecialTags
    {
        public int Id { get; set; }

        [Required]
        [Display(Name ="Special Tag")]
        public string SpecialTag { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

    }
}
