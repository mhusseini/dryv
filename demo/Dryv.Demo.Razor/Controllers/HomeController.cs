using Dryv.Demo.Razor.Models;
using Microsoft.AspNetCore.Mvc;

namespace Dryv.Demo.Razor.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View(new RegistrationModel());
    }

    [HttpPost]
    public IActionResult Index(RegistrationModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        return View("Success", model);
    }
}
