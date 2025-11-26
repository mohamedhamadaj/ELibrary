using System.ComponentModel.DataAnnotations;

namespace ELibrary.DTOs.Request
{
    public class ForgetPasswordRequest
    {
        [Required]
        public string UserNameOrEmail { get; set; } = string.Empty;
    }
}
