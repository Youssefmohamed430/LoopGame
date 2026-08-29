using Microsoft.AspNetCore.Mvc;

namespace CodeRunner.Controllers;

public class Executer : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}