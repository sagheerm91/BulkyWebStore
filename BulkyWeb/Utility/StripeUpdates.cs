using BulkyWeb.Data;
using BulkyWeb.Models;

namespace BulkyWeb.Utility
{
    public class StripeUpdates
    {
        private readonly ApplicationDbContext _context;
        public StripeUpdates(ApplicationDbContext context)
        {
            _context = context;
        }
        public void Update(OrderHeader obj) { 
            _context.OrderHeaders.Update(obj);
        }
        public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null) {
        var OrderFromDb = _context.OrderHeaders.FirstOrDefault(x => x.Id == id);
            if (OrderFromDb != null)
            {
                OrderFromDb.OrderStatus = orderStatus;
                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    OrderFromDb.PaymentStatus = paymentStatus;
                }
            }
        }
        public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
        {
            var orderFromDb = _context.OrderHeaders.FirstOrDefault(u=>u.Id == id);
            if ((!string.IsNullOrEmpty(sessionId)))
            {
                orderFromDb.SessionId = sessionId;
            }

            if ((!string.IsNullOrEmpty(paymentIntentId)))
            {
                orderFromDb.PaymentIntentId = paymentIntentId;
                orderFromDb.PaymentDate = DateTime.Now;
            }

        }
    }
}
