using Microsoft.EntityFrameworkCore;

namespace ELibrary.Repositories.IRepositories
{
    public interface IBookRepository : IRepository<Book>
    {
        Task AddRangeAsync(IEnumerable<Book>  books, CancellationToken cancellationToken = default);
    }
}
