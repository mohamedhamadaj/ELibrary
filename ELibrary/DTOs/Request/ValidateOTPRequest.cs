namespace ELibrary.DTOs.Request
{
    public class ValidateOTPRequest
    {
        public string OTP { get; set; } = string.Empty;
        public string ApplicationUserId { get; set; } = string.Empty;

    }
}
