using BulkyWeb.Data;
using BulkyWeb.Models;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> products = _db.Products.Include(u=>u.Category);
            
            return View(products);
        }
      //  [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            ShoppingCart cart = new()
            {
                Product = _db.Products.Include(u => u.Category).FirstOrDefault(u => u.Id == id),
                Count = 1,
                ProductId = id
            };
            return View(cart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            shoppingCart.Id = 0;
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            shoppingCart.ApplicationUserId = userId;

            ShoppingCart cartFromDb = _db.ShoppingCarts.FirstOrDefault(u => u.ApplicationUserId ==  userId &&
            u.ProductId == shoppingCart.ProductId);



            if(cartFromDb != null)
            {
                // Already Exists
                cartFromDb.Count += shoppingCart.Count;
                _db.Update(cartFromDb);
                _db.SaveChanges();
            }
            else
            {
                // Does not exist any cart & Add cart to Db
                _db.ShoppingCarts.Add(shoppingCart);
                _db.SaveChanges();
                HttpContext.Session.SetInt32(SD.SessionCart, _db.ShoppingCarts.Where(u=>u.ApplicationUserId == userId)
                    .Select(u => u.ProductId)
                         .Distinct()
                         .Count());
            }

            
            TempData["success"] = "Added To Cart Successfully";
            return RedirectToAction(nameof(Index));
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