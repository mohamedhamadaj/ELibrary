using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ELibrary.Areas.Admin
{
    [Area("Admin")]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IRepository<Category> _categoryRepository;
        public CategoriesController(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var categories = await _categoryRepository.GetAsync(tracked: false, cancellationToken: cancellationToken);

            return Ok(categories.AsEnumerable());
        }

        [HttpGet("GetOne/{id}")]
        [Authorize]
        public async Task<IActionResult> GetOne(int id, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetOneAsync(e => e.Id == id, cancellationToken: cancellationToken, tracked: false);

            if (category is null)
                return NotFound();

            return Ok(category);
        }

        [HttpPut("Edit/{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Edit(int id, Category category, CancellationToken cancellationToken)
        {
            var categoryInDb = await _categoryRepository.GetOneAsync(e => e.Id == id, cancellationToken: cancellationToken);
            if (categoryInDb is null)
                return NotFound();

            categoryInDb.Name = category.Name;
            categoryInDb.Description = category.Description;
            categoryInDb.Status = category.Status;

            await _categoryRepository.CommitAsync(cancellationToken);

            return NoContent();
        }

        [HttpPost("Create")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Create(Category category, CancellationToken cancellationToken)
        {
            await _categoryRepository.AddAsync(category, cancellationToken);
            await _categoryRepository.CommitAsync(cancellationToken);

            return CreatedAtAction(nameof(GetOne), new { id = category.Id }, new
            {
                Success_note = "Category created successfully.",
            });

        }

        [HttpDelete("Delete/{id}")]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> Delete(int id,CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetOneAsync(e => e.Id == id, cancellationToken: cancellationToken);
            if (category is null)
                return NotFound();
            _categoryRepository.Delete(category);
            await _categoryRepository.CommitAsync(cancellationToken);
             return NoContent();
        }
    }
}
