using System.Threading.Tasks;

namespace ELibrary.Repositories
{
    public class BookRepository : Repository<Book>, IBookRepository
    {
        private ApplicationDBContext _context;// = new();

        public BookRepository(ApplicationDBContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<Book> books, CancellationToken cancellationToken = default)
        {
            await _context.AddRangeAsync(books, cancellationToken);
        }
    }
}
