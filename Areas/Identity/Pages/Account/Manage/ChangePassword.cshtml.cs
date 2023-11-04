using Amazon.SimpleSystemsManagement.Model;
using Amazon.SimpleSystemsManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ratingsflex.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ratingsflex.Areas.Identity.Pages.Account;

namespace ratingsflex.Areas.Identity.Pages.Account.Manage
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<RatingsflexUser> _userManager;
        private readonly SignInManager<RatingsflexUser> _signInManager;
        private readonly IAmazonSimpleSystemsManagement _ssmClient;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(
            UserManager<RatingsflexUser> userManager,
            SignInManager<RatingsflexUser> signInManager,
            ILogger<ChangePasswordModel> logger,
            IAmazonSimpleSystemsManagement ssmClient)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _ssmClient = ssmClient;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string OldPassword { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToPage("./SetPassword");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }


            await _signInManager.RefreshSignInAsync(user);
            await UpdatePasswordInParameterStore(Input.OldPassword, Input.NewPassword);
            _logger.LogInformation("User changed their password successfully.");


            return RedirectToPage("Index");
        }

        private async Task UpdatePasswordInParameterStore(string oldPassword, string newPassword)
        {
            var username = User.Identity.Name;
            var sanitizedUsername = SanitizeUsername(username);
            var parameterName = $"/ratingsflex/credentials/{sanitizedUsername}";

            try
            {

                // Update the password in the Parameter Store
                var putParameterResponse = await _ssmClient.PutParameterAsync(new PutParameterRequest
                {
                    Name = parameterName,
                    Value = newPassword,
                    Type = ParameterType.SecureString,
                    Overwrite = true,
                });

                _logger.LogInformation("Successfully updated password in AWS SSM Parameter Store for user {username}.", username);
                TempData["SuccessMessage"] = "Your password has been changed.";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating password in AWS SSM Parameter Store for user {Username}: {ErrorMessage}", username, ex.Message);

            }
        }

        private static string SanitizeUsername(string username)
        {
            // Replace '@' with a placeholder, you can choose a different method of sanitization if required
            return username.Replace('@', '_').Replace('.', '-');
        }
    }

}