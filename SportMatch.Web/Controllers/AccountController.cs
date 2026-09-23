using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SportMatch.Web.Data.Entities;
using SportMatch.Web.Models.ViewModels;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;

namespace SportMatch.Web.Controllers;

public sealed class AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet]
    [Route("Admin/Login")]
    [Route("Account/Login")]
    public IActionResult Login() => User.IsInRole("Admin") ? RedirectToAction("Index", "Admin") : View(new LoginViewModel());

    [HttpPost]
    [EnableRateLimiting("admin-login")]
    [Route("Admin/Login")]
    [Route("Account/Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = model.Identifier.Contains('@')
            ? await userManager.FindByEmailAsync(model.Identifier)
            : await userManager.FindByNameAsync(model.Identifier);
        if (user is null || !await userManager.IsInRoleAsync(user, "Admin"))
        {
            ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không đúng.");
            return View(model);
        }
        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không đúng.");
            return View(model);
        }
        return RedirectToAction("Index", "Admin");
    }

    [HttpGet]
    public IActionResult Register() => RedirectToAction("Index", "Booking");

    [HttpPost]
    [Route("Admin/Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Redirect("/Admin/Login");
    }

    [Authorize(Roles = "Admin"), HttpGet]
    public IActionResult ChangePassword() => View(new AdminChangePasswordViewModel());

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(AdminChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Redirect("/Admin/Login");
        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        await signInManager.RefreshSignInAsync(user);
        TempData["PasswordChanged"] = "Đã đổi mật khẩu quản trị.";
        return RedirectToAction("Index", "Admin");
    }
}
