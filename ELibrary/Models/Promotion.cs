using ELibrary.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ELibrary.Models
{
    public class Promotion
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        [ValidateNever]
        public Book book { get; set; }
        public DateTime PublishAt { get; set; } = DateTime.UtcNow;
        public DateTime ValidTo { get; set; }
        public bool IsValid { get; set; } = true;

        public string Code { get; set; }
        public double Discount { get; set; }
    }
}
