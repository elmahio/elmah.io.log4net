using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Elmah.Io.Log4Net.AspNetCore31.Models;

namespace Elmah.Io.Log4Net.AspNetCore31.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogWarning("Request to frontpage");
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
                _logger.LogError(e, "An error happened");
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
