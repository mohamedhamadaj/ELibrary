using System.ComponentModel.DataAnnotations;

namespace ELibrary.DTOs.Request
{
    public class ResendEmailConfirmationRequest
    {
        [Required]
        public string UserNameOREmail { get; set; } = string.Empty;
    }
}
