using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ratingsflex.Areas.Identity.Data;

namespace ratingsflex.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }


    }
}
