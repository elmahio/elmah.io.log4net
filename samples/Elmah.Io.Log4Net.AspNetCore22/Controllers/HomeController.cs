using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Elmah.Io.Log4Net.AspNetCore22.Models;
using Microsoft.Extensions.Logging;
using System;

namespace Elmah.Io.Log4Net.AspNetCore22.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;

        public HomeController(ILogger<HomeController> logger)
        {
            this.logger = logger;
        }

        public IActionResult Index()
        {
            logger.LogWarning("Request to frontpage");
            return View();
        }

        public IActionResult Privacy()
        {
            try
            {
                int i = 0;
                var y = 12 / i;
            }
            catch (Exception e)
            {
                logger.LogError(e, "An error happened");
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
