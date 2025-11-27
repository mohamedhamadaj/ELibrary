using ELibrary.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace ELibrary.Repositories
{
    public class FavoriteRepository : Repository<Favorite>, IFavoriteRepository
    {
        private readonly ApplicationDBContext _context;

        public FavoriteRepository(ApplicationDBContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> Exists(string userId, int bookId)
        {
            return await _context.Favorites
                .AnyAsync(e => e.ApplicationUserId == userId && e.BookId == bookId);
        }
    }
}
