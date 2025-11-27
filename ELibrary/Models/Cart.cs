using ELibrary.Models;

namespace ELibrary.Models
{
    public class Cart
    {

        public int BookId { get; set; }
        public Book  book { get; set; }
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        public int Count { get; set; }
        public double Price { get; set; }
    }
}
