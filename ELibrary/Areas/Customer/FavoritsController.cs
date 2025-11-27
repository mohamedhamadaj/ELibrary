using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ELibrary.Areas.Customer
{
    [Area("Customer")]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class FavoritsController : ControllerBase
    {
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        public FavoritsController(IFavoriteRepository favoriteRepository, UserManager<ApplicationUser> userManager)
        {
            _favoriteRepository = favoriteRepository;
            _userManager = userManager;
        }


        [HttpPost("AddToFavorit")]
        public async Task<IActionResult> AddToFavorit(int bookId)
        {
           var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return NotFound();
            var favoritBook = await _favoriteRepository.Exists(user.Id, bookId);
            if (favoritBook)
                return BadRequest("Book is already in favorites.");

            await _favoriteRepository.AddAsync(new Favorite
            {
                ApplicationUserId = user.Id,
                BookId = bookId
            });
             await _favoriteRepository.CommitAsync();

            return Ok("Book added to favorites.");

        }

        [HttpGet("GetFavorites")]
        public async Task<IActionResult> GetFavorites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var favorites = await _favoriteRepository.GetAsync(
                e => e.ApplicationUserId == user.Id,
                includes: [e => e.Book]);

            return Ok(favorites);
        }


       
        [HttpDelete("Remove/{bookId}")]
        public async Task<IActionResult> Remove(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var favorite = await _favoriteRepository.GetOneAsync(
                e => e.ApplicationUserId == user.Id && e.BookId == bookId);

            if (favorite == null)
                return NotFound(new { msg = "Not found in favorites" });

            _favoriteRepository.Delete(favorite);
            await _favoriteRepository.CommitAsync();

            return Ok(new { msg = "Removed from favorites" });
        }

    }
}
