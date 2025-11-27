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

        [HttpPost("Get")]
        public async Task<IActionResult> GetAll(FilterBookRequest  filterBookRequest, CancellationToken cancellationToken, [FromQuery] int page = 1)
        {
            const decimal discount = 50;
            var books = await _bookRepository.GetAsync(includes: [e => e.Category], tracked: false, cancellationToken: cancellationToken);

            #region Filter Product
            FilterBookResponse filterProductResponse = new();

            // Add Filter 
            if (filterBookRequest.title is not null)
            {
                books = books.Where(e => e.Title.Contains(filterProductResponse.Title.Trim()));
                filterProductResponse.Title = filterBookRequest.title;
            }

            if (filterBookRequest.categoryId is not null)
            {
                books = books.Where(e => e.CategoryId == filterBookRequest.categoryId);
                filterProductResponse.CategoryId = filterBookRequest.categoryId;
            }

            if (filterBookRequest.lessQuantity)
            {
                books = books.OrderBy(e => e.Stock);
                filterProductResponse.LessQuantity = filterBookRequest.lessQuantity;
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
                Products = books.AsEnumerable(),
                FilterProductResponse = filterProductResponse,
                PaginationResponse = paginationResponse
            });
        }

        [HttpGet("GetOne /{id}")]
        
        public async Task<IActionResult> GetOne(int id, CancellationToken cancellationToken)
        {
            var book = await _bookRepository.GetOneAsync(e => e.Id == id, tracked: false, cancellationToken: cancellationToken);
            if (book is null)
            {
                return NotFound();
            }
            return Ok(book);
        }

        [HttpPost("Create")]
       
        public async Task<IActionResult> Create(CreateBookRequest createBookRequest, CancellationToken cancellationToken)
        {
            var transaction = _context.Database.BeginTransaction();
            Book book = createBookRequest.Adapt<Book>();

            try
            {
                if (createBookRequest.Img is not null && createBookRequest.Img.Length > 0)
                {
                    // Save Img in wwwroot
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(createBookRequest.Img.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\book_images", fileName);

                    using (var stream = System.IO.File.Create(filePath))
                    {
                        await createBookRequest.Img.CopyToAsync(stream);
                    }

                    // Save Img in db
                    book.Image = fileName;
                }

                var addedBook = await _bookRepository.AddAsync(book, cancellationToken);
                await _bookRepository.CommitAsync(cancellationToken);

                transaction.Commit();

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
