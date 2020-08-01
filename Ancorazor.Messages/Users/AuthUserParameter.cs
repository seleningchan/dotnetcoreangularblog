#region

#endregion

using System.ComponentModel.DataAnnotations;

namespace Ancorazor.API.Messages.Users
{
    public class AuthUserParameter : QueryParameter
    {
        [Required]
        [StringLength(30, MinimumLength = 4, ErrorMessage = "Username must be 4-30 characters")]
        public string LoginName { get; set; }

        [Required]
        //[StringLength(1, MinimumLength = 0, ErrorMessage = "Password must be 6-30 characters")]
        public string Password { get; set; }
    }

    public class ResetPasswordParameter : AuthUserParameter
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [Required]
        [StringLength(256, MinimumLength = 6, ErrorMessage = "Password must be 6-30 characters")]
        public string NewPassword { get; set; }
    }
}