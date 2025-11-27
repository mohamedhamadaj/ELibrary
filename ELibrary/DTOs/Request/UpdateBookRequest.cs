namespace ELibrary.DTOs.Request
{
    public class UpdateBookRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public double Price { get; set; }
        public decimal Discount { get; set; }
        public string? year { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }

        public IFormFile? Img { get; set; } = default!;
    }
}
