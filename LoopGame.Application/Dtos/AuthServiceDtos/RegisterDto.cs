using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Dtos.AuthServiceDtos
{
    public class RegisterDto
    {
        [Required]
        public string UserName { get; set; } = null!;
        [Required]
        public string? Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
        [Required]
        [MinLength(10, ErrorMessage = "At Least ten Letters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
