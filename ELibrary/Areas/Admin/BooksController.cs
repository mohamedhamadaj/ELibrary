using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELibrary.Areas.Admin
{
    [Area("Admin")]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDBContext _context;// = new();
        private readonly IBookRepository _bookRepository;
        private readonly IRepository<Category> _categoryRepository;// = new();
        private readonly ILogger<BooksController> _logger;

        public BooksController(ApplicationDBContext context, IBookRepository bookRepository,
            IRepository<Category> categoryRepository, ILogger<BooksController> logger)
        {
            _context = context;
            _bookRepository = bookRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(FilterBookRequest filterBookRequest, CancellationToken cancellationToken, [FromQuery] int page = 1)
        {

            var books = await _bookRepository.GetAsync(includes: [e => e.Category, e => e.Image], tracked: false, cancellationToken: cancellationToken);

            #region Filter Book
            FilterBookResponse filterBookResponse = new();

            // Add Filter 
            if (filterBookRequest.title is not null)
            {
                books = books.Where(e => e.Title.Contains(filterBookRequest.title.Trim()));
                filterBookResponse.Title = filterBookRequest.title;
            }

            if (filterBookRequest.publisher is not null)
            {
                books = books.Where(e => e.Author == filterBookResponse.Publisher);
                filterBookResponse.Publisher = filterBookRequest.publisher;
            }

            if (filterBookRequest.categoryId is not null)
            {
                books = books.Where(e => e.CategoryId == filterBookRequest.categoryId);
                filterBookResponse.CategoryId = filterBookRequest.categoryId;
            }

            if (filterBookRequest.lessQuantity)
            {
                books = books.OrderBy(e => e.Stock);
                filterBookResponse.LessQuantity = filterBookRequest.lessQuantity;
            }
            if (filterBookRequest.year is not null)
            {
                books = books.OrderBy(e => e.Year);
                filterBookResponse.year = filterBookRequest.year;
            }

            #endregion

            #region Pagination
            PaginationResponse paginationResponse = new();

            // Pagination
            paginationResponse.TotalPages = Math.Ceiling(books.Count() / 8.0);
            paginationResponse.CurrentPage = page;
            books = books.Skip((page - 1) * 8).Take(8); // 0 .. 8 
            #endregion

            return Ok(new
            {
                Books = books.AsEnumerable(),
                FilterBookResponse = filterBookResponse,
                PaginationResponse = paginationResponse
            });
        }

        [HttpGet("GetOne /{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> GetOne(int id, CancellationToken cancellationToken)
        {
            var book = await _bookRepository.GetOneAsync(e => e.Id == id, includes: [e => e.Category, e => e.Image], tracked: false, cancellationToken: cancellationToken);
            if (book is null)
            {
                return NotFound();
            }
            return Ok(book);
        }

        [HttpPost("Create")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Create(CreateBookRequest createBookRequest, CancellationToken cancellationToken)
        {
            var transaction = _context.Database.BeginTransaction();
            Book book = createBookRequest.Adapt<Book>();

            try
            {
                if (createBookRequest.Img is not null && createBookRequest.Img.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(createBookRequest.Img.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\book_images", fileName);

                    using (var streem = System.IO.File.Create(filePath))
                    {
                        await createBookRequest.Img.CopyToAsync(streem);
                    }
                    //save in DB
                    book.Image = fileName;
                }

                var addedBook = await _bookRepository.AddAsync(book, cancellationToken);
                await _bookRepository.CommitAsync();

                await transaction.CommitAsync(cancellationToken);
                return CreatedAtAction(nameof(GetOne), new { id = addedBook.Id }, new
                {
                    msg = "Add Book Successfully "
                });

            }
            catch (Exception ex)
            {
                // Logging
                _logger.LogError(ex.Message);
                transaction.Rollback();

                // Validation
                return BadRequest(new ErrorModelResponse
                {
                    Code = "Error While Saving the product",
                    Description = ex.Message,
                });
            }
        }

        [HttpPut("Edit/{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Edit(int id, UpdateBookRequest updateBookRequest, CancellationToken cancellationToken)
        {
            var bookInDb = await _bookRepository.GetOneAsync(e => e.Id == id, cancellationToken: cancellationToken);

            if (bookInDb is null)
                return NotFound();

            if (updateBookRequest.Img is not null)
            {
                if (updateBookRequest.Img.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(updateBookRequest.Img.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\book_images", fileName);
                    using (var streem = System.IO.File.Create(filePath))
                    {
                        await updateBookRequest.Img.CopyToAsync(streem);
                    }

                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\book_images", bookInDb.Image);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                    bookInDb.Image = fileName;
                }
            }
            bookInDb.Title = updateBookRequest.Title;
            bookInDb.Description = updateBookRequest.Description;
            bookInDb.IsActive = updateBookRequest.IsActive;
            bookInDb.Price = updateBookRequest.Price;
            bookInDb.Discount = updateBookRequest.Discount;
            bookInDb.Stock = updateBookRequest.Stock;
            bookInDb.CategoryId = updateBookRequest.CategoryId;

            _bookRepository.Update(bookInDb);
            await _bookRepository.CommitAsync(cancellationToken);


            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var bookInDb = await _bookRepository.GetOneAsync(e => e.Id == id, includes: [e => e.Image], cancellationToken: cancellationToken);
            
            if (bookInDb is null)
                return NotFound();

            // Remove old Img in wwwroot
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\book_images", bookInDb.Image);
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);

            _bookRepository.Delete(bookInDb);
            await _bookRepository.CommitAsync(cancellationToken);

            return NoContent();
        }
    }
}
