using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ratingsflex.Areas.Identity.Data;
using Microsoft.Extensions.Logging;
using ratingsflex.Areas.Identity.Pages.Account;

namespace ratingsflex.Areas.Identity.Controllers
{
    public class ProfileController : Controller  // Inherit from Controller
    {
        private readonly UserManager<ratingsflexUser> _userManager;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(UserManager<ratingsflexUser> userManager, ILogger<ProfileController> logger)  // Constructor
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UpdateProfileModel.InputModel model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var modelStateValue in ModelState.Values)
                {
                    foreach (var error in modelStateValue.Errors)
                    {
                        _logger.LogError(error.ErrorMessage);
                    }
                }
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.Firstname = model.FirstName;
                user.Lastname = model.LastName;
                user.PhoneNumber = model.Phone;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User updated their profile.");

                    // Add a success message to TempData
                    TempData["SuccessMessage"] = "Your profile has been updated successfully.";

                    return RedirectToPage("/Account/Manage/Index", new { Area = "Identity" });

                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "User not found.");
            }

            return View(model);
        }


        private void SaveCredentialsInParameterStore(string username, string password)
        {
            // Implement the logic to save credentials in AWS SSM Parameter Store
        }
    }
}
