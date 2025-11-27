using ELibrary.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ELibrary.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {


        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IRepository<ApplicationUserOTP> _applicationUserOTPRepository;
        private readonly ITokenService _tokenService;

        public AccountsController(UserManager<ApplicationUser> userManager, IEmailSender emailSender, SignInManager<ApplicationUser> signInManager, IRepository<ApplicationUserOTP> applicationUserOTPRepository, ITokenService tokenService)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _signInManager = signInManager;
            _applicationUserOTPRepository = applicationUserOTPRepository;
            _tokenService = tokenService;
        }


        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterRequest registerRequest)
        {
            var user = new ApplicationUser()
            {
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                Email = registerRequest.Email,
                UserName = registerRequest.Username,
            };

            var result = await _userManager.CreateAsync(user, registerRequest.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            //send email confirmation link logic can be added here
            var Token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Accounts", new { area = "Identity", Token, useId = user.Id }, Request.Scheme);

            await _emailSender.SendEmailAsync(registerRequest.Email, "ELibrary - Confirm Your Email!"
                , $"<h1>Confirm Your Email By Clicking <a href='{link}'>Here</a></h1>");

            await _userManager.AddToRoleAsync(user, SD.CUSTOMER_ROLE);



            return Ok(new
            {

                Message = "Create Account Successfully"
            });
        }

        [HttpPost("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return BadRequest("Invalid User");
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                return BadRequest("Email Confirmation Failed");
            }

            else
                return Ok(new
                {
                    msg = "Confirm Email Successfully"
                });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            var user = await _userManager.FindByNameAsync(loginRequest.UserNameOrEmail) ?? await _userManager.FindByEmailAsync(loginRequest.UserNameOrEmail);

            if (user is null)
            {
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginRequest.Password, loginRequest.RememberMe, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    return BadRequest(new ErrorModelResponse
                    {
                        Code = "Too many attemps",
                        Description = "Too many attemps, try again after 5 min"
                    });
                else if (!user.EmailConfirmed)
                    return BadRequest(new ErrorModelResponse
                    {
                        Code = "Confirm Your Email",
                        Description = "Please Confirm Your Email First!!"
                    });
                else
                    return NotFound(new ErrorModelResponse
                    {
                        Code = "Invalid Cred.",
                        Description = "Invalid User Name / Email OR Password"
                    });
            }

            //Generate JWT Token
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Role, String.Join(", ", userRoles)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var accessToken = _tokenService.GenerateAccessToken(claims);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userManager.UpdateAsync(user);
            return Ok(new
            {
                AccessToken = accessToken,
                ValidTo = "30 min",
                RefreshToken = refreshToken,
                RefreshTokenExpiration = "7 day"
            });
        }

        [HttpPost("ResendEmailConfirmation")]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationRequest resendEmailConfirmationRequest)
        {
            var user = await _userManager.FindByNameAsync(resendEmailConfirmationRequest.UserNameOREmail);
            if (user == null)
            {
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });
            }

            if (user.EmailConfirmed)
            {
                return BadRequest(new ErrorModelResponse
                {
                    Code = "Email Already Confirmed",
                    Description = "Your Email is already confirmed."
                });
            }

            // Send Confirmation Mail
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account", new { area = "Identity", token, userId = user.Id }, Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email!, "Ecommerce 519 - Resend Confirm Your Email!"
                , $"<h1>Confirm Your Email By Clicking <a href='{link}'>Here</a></h1>");

            return Ok(new
            {
                msg = "Send msg successfully"
            });
        }

        [HttpPost("ForgetPassword")]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordRequest forgetPasswordRequest)
        {
            var user = await _userManager.FindByNameAsync(forgetPasswordRequest.UserNameOrEmail) ??
               await _userManager.FindByEmailAsync(forgetPasswordRequest.UserNameOrEmail);
            if (user is null)
            {
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });
            }

            var userOTPs = await _applicationUserOTPRepository.GetAsync(u => u.ApplicationUserId == user.Id);
            var totalOTPs = userOTPs.Count(e => (DateTime.UtcNow - e.CreateAt).TotalHours < 24);
            if (totalOTPs > 3)
            {
                return BadRequest(new ErrorModelResponse
                {
                    Code = "Too many attemps",
                    Description = "Too many attemps, try again later"
                });
            }

            var otp = new Random().Next(1000, 9999).ToString();
            await _applicationUserOTPRepository.AddAsync(new()
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationUserId = user.Id,
                OTP = otp,
                CreateAt = DateTime.UtcNow,
                IsValid = true,
                ValidTo = DateTime.UtcNow.AddMinutes(15)
            });
            await _applicationUserOTPRepository.CommitAsync();
            await _emailSender.SendEmailAsync(user.Email!, "ELibrary - Reset Your Password"
                , $"<h1>Use This OTP: {otp} To Reset Your Account. Don't share it.</h1>");

            return CreatedAtAction("ValidateOTP", new { userId = user.Id });
        }


        [HttpPost("ValidateOTP")]
        public async Task<IActionResult> ValidateOTP(ValidateOTPRequest validateOTPRequest)
        {
            var result = await _applicationUserOTPRepository.GetOneAsync(e => e.ApplicationUserId == validateOTPRequest.ApplicationUserId && e.OTP == validateOTPRequest.OTP && e.IsValid);
            if (result is null)
            {
                return CreatedAtAction("ValidateOTP", new { userId = validateOTPRequest.ApplicationUserId });
            }

            return CreatedAtAction("ValidateOTP", new { userId = validateOTPRequest.ApplicationUserId });
           
        }

        [HttpPost("NewPassword")]
        public async Task<IActionResult> NewPassword(NewPasswordRequest newPasswordRequest)
        {
            var user = await _userManager.FindByIdAsync(newPasswordRequest.ApplicationUserId);

            if (user is null)
            {
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, token, newPasswordRequest.Password);


            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok();
        }

        //[HttpPost]
        //[Route("refresh")]
        //public async Task<IActionResult> Refresh(TokenApiRequest tokenApiRequest)
        //{
        //    if (tokenApiRequest is null || tokenApiRequest.RefreshToken is null || tokenApiRequest.AccessToken is null)
        //        return BadRequest("Invalid client request");

        //    string accessToken = tokenApiRequest.AccessToken;
        //    string refreshToken = tokenApiRequest.RefreshToken;

        //    var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);

        //    var userName = principal.Identity.Name;
        //    var user = _userManager.Users.FirstOrDefault(e => e.UserName == userName);

        //    if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
        //        return BadRequest("Invalid client request");

        //    var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
        //    var newRefreshToken = _tokenService.GenerateRefreshToken();

        //    user.RefreshToken = newRefreshToken;
        //    await _userManager.UpdateAsync(user);

        //    return Ok(new
        //    {
        //        AccessToken = newAccessToken,
        //        ValidTo = "30 min",
        //        RefreshToken = newRefreshToken,
        //    });
        //}

        //[HttpPost, Authorize]
        //[Route("Revoke")]
        //public async Task<IActionResult> Revoke()
        //{
        //    var username = User.Identity.Name;
        //    var user = _userManager.Users.FirstOrDefault(e => e.UserName == username);
        //    if (user == null) return BadRequest();
        //    user.RefreshToken = null;
        //    await _userManager.UpdateAsync(user);
        //    return NoContent();
        //}
    }
}
