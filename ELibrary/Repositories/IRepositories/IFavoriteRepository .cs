namespace ELibrary.Repositories.IRepositories
{
    public interface IFavoriteRepository : IRepository<Favorite>
    {
        Task<bool> Exists(string userId, int bookId);

    }
}
