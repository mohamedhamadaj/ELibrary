namespace ELibrary.DTOs.Response
{
    public class FilterBookResponse
    {
        public string? Title { get; set; }
        public string? Publisher { get; set; }
        public string? Description { get; set; }
        public int? discount { get; set; }
        public string? year { get; set; }
        public int? CategoryId { get; set; }
        public bool LessQuantity { get; set; }
        public double? Price { get; set; }
    }
}
