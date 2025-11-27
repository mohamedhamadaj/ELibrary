using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ELibrary.Areas.Admin
{
    [Area("Admin")]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("GetAll")]
        public IActionResult GetAllUsers()
        {
            
            return Ok(_userManager.Users.Adapt<IEnumerable<UserResponse>>());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> LockUnLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user is null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, SD.SUPER_ADMIN_ROLE))
            {
                return BadRequest(new ErrorModelResponse()
                {
                    Code = "Error",
                    Description = "You can not block super admin account"
                });
            }

            user.LockoutEnabled = !user.LockoutEnabled;

            if (!user.LockoutEnabled)
                user.LockoutEnd = DateTime.UtcNow.AddDays(10);
            else
                user.LockoutEnd = null;

            await _userManager.UpdateAsync(user);

            return NoContent();
        }

    }
}
