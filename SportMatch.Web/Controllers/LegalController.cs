using Microsoft.AspNetCore.Mvc;

namespace SportMatch.Web.Controllers;

public sealed class LegalController : Controller
{
    public IActionResult Privacy() => View();
    public IActionResult Terms() => View();
    public IActionResult Cancellation() => View();
}
