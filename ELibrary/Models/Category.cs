
using System.ComponentModel.DataAnnotations;

namespace ELibrary.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        [CustomLength( 3,100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Description { get; set; }
        public bool Status { get; set; }
    }
}
