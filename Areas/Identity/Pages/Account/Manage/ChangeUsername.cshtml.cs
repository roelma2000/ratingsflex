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
    public class ChangeUsernameModel : PageModel
    {
        private readonly UserManager<RatingsflexUser> _userManager;
        private readonly SignInManager<RatingsflexUser> _signInManager;
        private readonly IAmazonSimpleSystemsManagement _ssmClient;
        private readonly ILogger<ChangeUsernameModel> _logger;

        public ChangeUsernameModel(UserManager<RatingsflexUser> userManager, SignInManager<RatingsflexUser> signInManager, ILogger<ChangeUsernameModel> logger, IAmazonSimpleSystemsManagement ssmClient)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _ssmClient = ssmClient;
            _logger = logger;
        }

        [BindProperty]
        [Required]
        public string Username { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            Username = user.UserName;
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
            var oldUsername = user.UserName;
            var setUserNameResult = await _userManager.SetUserNameAsync(user, Username);
            if (!setUserNameResult.Succeeded)
            {
                foreach (var error in setUserNameResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }
            await _signInManager.RefreshSignInAsync(user);
            UpdateUsernameInParameterStore(oldUsername, Username);
            return RedirectToPage("Index");
        }

        private void UpdateUsernameInParameterStore(string oldUsername, string newUsername)
        {
            var oldSanitizedUsername = SanitizeUsername(oldUsername);
            var newSanitizedUsername = SanitizeUsername(newUsername);

            var oldParameterName = $"/ratingsflex/credentials/{oldSanitizedUsername}";
            var newParameterName = $"/ratingsflex/credentials/{newSanitizedUsername}";

            try
            {
                // Copy the value from the old parameter to the new parameter
                var getParameterResponse = _ssmClient.GetParameterAsync(new GetParameterRequest
                {
                    Name = oldParameterName,
                    WithDecryption = true,
                }).Result;
                var value = getParameterResponse.Parameter.Value;

                var putParameterResponse = _ssmClient.PutParameterAsync(new PutParameterRequest
                {
                    Name = newParameterName,
                    Value = value,
                    Type = ParameterType.SecureString,
                    Overwrite = true,
                }).Result;

                // Delete the old parameter
                var deleteParameterResponse = _ssmClient.DeleteParameterAsync(new DeleteParameterRequest
                {
                    Name = oldParameterName,
                }).Result;

                _logger.LogInformation("Successfully updated username in AWS SSM Parameter Store from {oldUsername} to {newUsername}.", oldUsername, newUsername);
                // Add a success message to TempData
                TempData["SuccessMessage"] = "Your username has been updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating username in AWS SSM Parameter Store from {oldUsername} to {newUsername}: {Message}", oldUsername, newUsername, ex.Message);
            }
        }

        private static string SanitizeUsername(string username)
        {
            // Replace '@' with a placeholder, you can choose a different method of sanitization if required
            return username.Replace('@', '_').Replace('.', '-');
        }

    }

}