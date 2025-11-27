using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Threading.Tasks;

namespace ELibrary.Areas.Customer
{
    [Area("Customer")]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<Promotion> _promotionRepository;
        private readonly IBookRepository _bookRepository;

        public CartsController(UserManager<ApplicationUser> userManager, IRepository<Cart> cartRepository, IRepository<Promotion> promotionRepository, IBookRepository bookRepository)
        {
            _userManager = userManager;
            _cartRepository = cartRepository;
            _promotionRepository = promotionRepository;
            _bookRepository = bookRepository;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return NotFound();

            var cart = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id,
                includes: [e => e.book, e => e.ApplicationUser]);
            var promotion = await _promotionRepository.GetOneAsync(e => e.Code == code && e.IsValid);

            if (promotion is not null)
            {
                var result = cart.FirstOrDefault(e => e.BookId == promotion.BookId);

                if (result is not null)
                    result.Price -= result.book.Price * (promotion.Discount / 100);

                await _cartRepository.CommitAsync();
            }
            return Ok(cart);
        }

        [HttpPost("AddToCart/{bookId}")]
        public async Task<IActionResult> AddToCart(int bookId, int count, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            var bookInDb = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.BookId == bookId);
            if (bookInDb is not null)
            {
                bookInDb.Count += count;
                await _cartRepository.CommitAsync();
                return Ok(new { msg = "Product count updated successfully" });
            }

            await _cartRepository.AddAsync(new()
            {
                BookId = bookId,
                Count = count,
                ApplicationUserId = user.Id,
                Price = (await _bookRepository.GetOneAsync(e => e.Id == bookId))!.Price
            }, cancellationToken: cancellationToken);

            await _cartRepository.CommitAsync();

            return Ok(new { msg = "Product added to cart successfully" });
        }

        [HttpPut("IncrementBook/{bookId}")]
        public async Task<IActionResult> IncrementProduct(int bookId, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            var book = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.BookId == bookId);
            if (book is null) return NotFound();

            book.Count += 1;
            await _cartRepository.CommitAsync();

            return Ok(new { msg = "Product count incremented successfully" });
        }
        [HttpPut("DeccrementBook/{bookId}")]
        public async Task<IActionResult> DecrementProduct(int bookId, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            var book = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.BookId == bookId);
            if (book is null) return NotFound();

            if (bookId <= 1)
                _cartRepository.Delete(book);
            else
                book.Count -= 1;

            await _cartRepository.CommitAsync();

            return Ok(new { msg = "Product count Decremented successfully" });
        }

        [HttpDelete("RemoveFromCart/{bookId}")]
        public async Task<IActionResult> RemoveFromCart(int bookId, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();
            
            var book = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.BookId == bookId);
           
            if (book is null) return NotFound();

            _cartRepository.Delete(book);
            await _cartRepository.CommitAsync();

            return Ok(new { msg = "Product removed from cart successfully" });
        }

        [HttpPost("Checkout")]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            var cart = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id, includes: [e => e.BookId]);
            if (cart is null)
                return NotFound();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/success",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/cancel",
            };

            foreach (var item in cart)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "egp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.book.Title,
                            Description = item.book.Description,
                        },
                        UnitAmount = (long)item.Price * 100,
                    },
                    Quantity = item.Count,
                });
            }

            var service = new SessionService();
            var session = service.Create(options);
            return Redirect(session.Url);
        }



    }
}
