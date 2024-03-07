using BulkyWeb.Data;
using BulkyWeb.Models;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Controllers
{

    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private StripeUpdates stripeUpdates { get; set; }
        [BindProperty]
        public OrderVM orderVM { get; set; }
        public OrderController(ApplicationDbContext db)
        {
            _db = db;
            stripeUpdates = new StripeUpdates(db);
        }
        //[Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            orderVM = new()
            {
                OrderHeader = _db.OrderHeaders.Include("ApplicationUser").FirstOrDefault(u => u.Id == orderId),
                OrderDetail = _db.OrderDetails.Include(od => od.Product).
                FirstOrDefault(u => u.OrderHeaderId == orderId),
            };
            return View(orderVM);
        }

        [HttpPost]
        [Authorize(Roles = "Admin" + "," + "Employee")]
        public IActionResult UpdateOrderDetails()
        {
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == orderVM.OrderHeader.Id);
            orderFromDb.Name = orderVM.OrderHeader.Name;
            orderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
            orderFromDb.City = orderVM.OrderHeader.City;
            orderFromDb.State = orderVM.OrderHeader.State;
            orderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
            orderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;

            if (!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
            {
                orderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            }

            if (!string.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber))
            {
                orderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            }

            _db.OrderHeaders.Update(orderFromDb);
            _db.SaveChanges();

            TempData["Success"] = "Order details updated successfully";

            return RedirectToAction(nameof(Details), new { orderId = orderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin" + "," + "Employee")]
        public IActionResult StartProcessingOrder()
        {
            stripeUpdates.UpdateStatus(orderVM.OrderHeader.Id, SD.StatusInProcess);
            _db.SaveChanges();
            TempData["Success"] = "Order details updated successfully";
            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }

        // Shipping Order
        [HttpPost]
        [Authorize(Roles = "Admin" + "," + "Employee")]
        public IActionResult ShipOrder()
        {
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == orderVM.OrderHeader.Id);
            orderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            orderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            orderFromDb.OrderStatus = SD.StatusShipped;
            orderFromDb.ShippingDate = DateTime.Now;

            if (orderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _db.Update(orderFromDb);
            _db.SaveChanges();
            TempData["Success"] = "Order shipped successfully";
            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }

        // Cancel Order
        [HttpPost]
        [Authorize(Roles = "Admin" + "," + "Employee")]
        public IActionResult CancelOrder()
        {
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == orderVM.OrderHeader.Id);
            stripeUpdates = new StripeUpdates(_db);

            if (orderFromDb.OrderStatus == SD.PaymentStatusApproved)
            {
                var option = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderFromDb.PaymentIntentId
                };
                var service = new RefundService();
                Refund refund = service.Create(option);

                
                stripeUpdates.UpdateStatus(orderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                stripeUpdates.UpdateStatus(orderFromDb.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            _db.SaveChanges();
            TempData["Success"] = "Order cancelled successfully";

            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }

        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_Pay_Now()
        {
            orderVM.OrderHeader = _db.OrderHeaders.Include("ApplicationUser").
                FirstOrDefault(u=>u.Id == orderVM.OrderHeader.Id);

            

            orderVM.OrderDetail = _db.OrderDetails.Include("Product").
                FirstOrDefault(u=>u.OrderHeaderId == orderVM.OrderHeader.Id);

            orderVM.OrderDetailList = _db.OrderDetails.Include("Product").Where(u => u.OrderHeaderId == orderVM.OrderDetail.OrderHeaderId).ToList();

            var domain = "https://localhost:7085/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"order/PaymentConfirmation?orderHeaderId={orderVM.OrderHeader.Id}",
                CancelUrl = domain + $"order/index/details?orderHeaderId={orderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            

            foreach (var item in orderVM.OrderDetailList)
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
            var service = new SessionService();
            Session session = service.Create(options);
            stripeUpdates.UpdateStripePaymentId(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _db.SaveChanges();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _db.OrderHeaders.FirstOrDefault(x => x.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                // this order is placed by company
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    stripeUpdates.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    stripeUpdates.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _db.SaveChanges();
                }
            }

            return View(orderHeaderId);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {

            IEnumerable<OrderHeader> objOrderHeader = _db.OrderHeaders.Include("ApplicationUser").ToList();

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            if (!(User.IsInRole("Admin") || User.IsInRole("Employee")))
            {
                objOrderHeader = objOrderHeader.Where(u => u.ApplicationUserId == userId).ToList();
            }

            switch (status)
            {
                case "inprocess":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "pending":
                    objOrderHeader = objOrderHeader.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "approved":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                case "completed":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                default:
                    break;
            }

            return Json(new { data = objOrderHeader });
        }
        #endregion
    }
}
