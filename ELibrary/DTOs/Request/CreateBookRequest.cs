namespace ELibrary.DTOs.Request
{
    public class CreateBookRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Status { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public IFormFile Img { get; set; } = default!;
    }
}
