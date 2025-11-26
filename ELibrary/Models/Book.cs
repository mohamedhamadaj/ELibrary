using ELibrary.Validations;
using System.ComponentModel.DataAnnotations;

namespace ELibrary.Models
{
    public class Book
    {
        public int Id { get; set; }
        [Required]
        [CustomLength(3, 100)]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Author { get; set; } = string.Empty;
        public double Price { get; set; }
        public decimal Rating { get; set; }
        public int Stock { get; set; }
        public decimal Discount { get; set; }
        [MaxLength(1000)]
        public string? Description { get; set; }
        public DateTime Created { get; set; }
        public string Image { get; set; } = string.Empty;
        public long Traffic { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }


    }
}
