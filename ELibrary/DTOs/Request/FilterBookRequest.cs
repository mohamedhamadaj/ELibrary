namespace ELibrary.DTOs.Request
{
    public record FilterBookRequest(
        string title, string publisher, int? categoryId ,string year, bool lessQuantity
        );
}
