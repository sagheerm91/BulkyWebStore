using BulkyWeb.Data;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyWeb.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        public ShoppingCartViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if(userId != null)
            {
                if(HttpContext.Session.GetInt32(SD.SessionCart) == null)
                {
                    HttpContext.Session.SetInt32(SD.SessionCart, _context.ShoppingCarts
                .Where(u => u.ApplicationUserId == userId.Value)
                .Select(u => u.ProductId)
                .Distinct()
                .Count());

                   /* int uniqueItemCount = _context.ShoppingCarts
                .Where(u => u.ApplicationUserId == userId)
                .Select(u => u.ProductId)
                .Distinct()
                .Count(); */
                }

                return View(HttpContext.Session.GetInt32(SD.SessionCart));
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }

            

        }

    }

    
}
