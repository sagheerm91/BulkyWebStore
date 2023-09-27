using BulkyWeb.Data;
using BulkyWeb.Models;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.BillingPortal;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private StripeUpdates stripeUpdates {  get; set; }
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(ApplicationDbContext db)
        {
            _db = db;
            stripeUpdates = new StripeUpdates(db);
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            IEnumerable<ShoppingCart> shoppingList = _db.ShoppingCarts.Include(i => i.Product);

            IEnumerable<ShoppingCart> userShoppingCarts = shoppingList
                .Where(cart => cart.ApplicationUserId == userId).ToList();

            ShoppingCartVM = new()
            {
                ShoppingCartList = userShoppingCarts,
                OrderHeader = new()

            };

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }


        // Increase Qty
        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _db.ShoppingCarts.FirstOrDefault(cart => cart.Id == cartId);
            cartFromDb.Count += 1;
            _db.Update(cartFromDb);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // Decrease Qty
        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _db.ShoppingCarts.FirstOrDefault(cart => cart.Id == cartId);
            if (cartFromDb.Count <= 1)
            {
                _db.Remove(cartFromDb);
                HttpContext.Session.SetInt32(SD.SessionCart, _db.ShoppingCarts.Where(u => u.ApplicationUserId == cartFromDb.ApplicationUserId)
                    .Select(u => u.ProductId)
                         .Distinct()
                         .Count() - 1);
            }
            else
            {
                cartFromDb.Count -= 1;
                _db.Update(cartFromDb);
            }
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _db.ShoppingCarts.FirstOrDefault(cart => cart.Id == cartId);
            _db.Remove(cartFromDb);
            HttpContext.Session.SetInt32(SD.SessionCart, _db.ShoppingCarts.Where(u => u.ApplicationUserId == cartFromDb.ApplicationUserId)
                    .Select(u => u.ProductId)
                         .Distinct()
                         .Count() - 1);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // Summary Page
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            IEnumerable<ShoppingCart> shoppingList = _db.ShoppingCarts.Include(i => i.Product);

            IEnumerable<ShoppingCart> userShoppingCarts = shoppingList
                .Where(cart => cart.ApplicationUserId == userId).ToList();

            ShoppingCartVM = new()
            {
                ShoppingCartList = userShoppingCarts,
                OrderHeader = new()

            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _db.ApplicationUsers.FirstOrDefault(i => i.Id == userId);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            IEnumerable<ShoppingCart> shoppingList = _db.ShoppingCarts.Include(i => i.Product);

            IEnumerable<ShoppingCart> userShoppingCarts = shoppingList
                .Where(cart => cart.ApplicationUserId == userId).ToList();

            ShoppingCartVM = new ShoppingCartVM
            {
                ShoppingCartList = userShoppingCarts,
                OrderHeader = new OrderHeader()
            };

            // Assuming your view has form fields that correspond to the OrderHeader properties
            // Make sure these field names match the properties in OrderHeader
            ApplicationUser applicationUser = _db.ApplicationUsers.FirstOrDefault(i => i.Id == userId);
            ShoppingCartVM.OrderHeader.ApplicationUser = _db.ApplicationUsers.FirstOrDefault(i => i.Id == userId);

            ShoppingCartVM.OrderHeader.Name = Request.Form["OrderHeader.Name"];
            ShoppingCartVM.OrderHeader.PhoneNumber = Request.Form["OrderHeader.PhoneNumber"];
            ShoppingCartVM.OrderHeader.StreetAddress = Request.Form["OrderHeader.StreetAddress"];
            ShoppingCartVM.OrderHeader.City = Request.Form["OrderHeader.City"];
            ShoppingCartVM.OrderHeader.PostalCode = Request.Form["OrderHeader.PostalCode"];
            ShoppingCartVM.OrderHeader.State = Request.Form["OrderHeader.State"];

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }


            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // Not a company account
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            }
            else
            {
                // Company Account
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            }

            // You should validate and handle user input and model binding errors here.

            // Add the OrderHeader to the database
            _db.OrderHeaders.Add(ShoppingCartVM.OrderHeader);
            _db.SaveChanges();
            HttpContext.Session.Clear();

            // Add OrderDetails to the database
            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new OrderDetail
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count,
                };
                _db.OrderDetails.Add(orderDetail);
            }
            _db.SaveChanges();

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // Stripe Logic Starts Here
                var domain = "https://localhost:7085/";
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    SuccessUrl = domain + $"cart/OrderConfirmation/?id={ShoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + "cart/index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach(var item in ShoppingCartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }
                var service = new Stripe.Checkout.SessionService();
                Stripe.Checkout.Session session = service.Create(options);
                stripeUpdates.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _db.SaveChanges();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }

            
            if (userId != null)
            {
                    int uniqueItemCount = _db.ShoppingCarts
                .Where(u => u.ApplicationUserId == userId)
                .Select(u => u.ProductId)
                .Distinct()
                .Count();                
            }

            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
        }


        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _db.OrderHeaders.Include("ApplicationUser").FirstOrDefault(x => x.Id == id);
            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                // this order is placed by customer
                var service = new Stripe.Checkout.SessionService();
                Stripe.Checkout.Session session = service.Get(orderHeader.SessionId);

                if(session.PaymentStatus.ToLower() == "paid")
                {
                    stripeUpdates.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    stripeUpdates.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _db.SaveChanges();
                    HttpContext.Session.Clear();
                }
            }

            IEnumerable<ShoppingCart> shoppingList = _db.ShoppingCarts.Include(i => i.Product);

            IEnumerable<ShoppingCart> userShoppingCarts = shoppingList
                .Where(cart => cart.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _db.RemoveRange(userShoppingCarts);
            _db.SaveChanges();

            return View(id);
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else if (shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }
            else
            {
                return shoppingCart.Product.Price100;
            }
        }
    }
}
