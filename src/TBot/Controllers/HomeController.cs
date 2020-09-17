using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TBot.Models;
using TBot.Service;

namespace TBot.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStateService _stateService;

        public HomeController(IStateService stateService)
        {
            _stateService = stateService;
        }

        public async Task<IActionResult> Index()
        {
            var state = await _stateService.GetAsync();
            return View(state);
        }

        public async Task<IActionResult> GetState()
        {
            var state = await _stateService.GetAsync();
            return Ok(state);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
