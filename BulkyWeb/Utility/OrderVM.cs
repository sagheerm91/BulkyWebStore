using BulkyWeb.Models;

namespace BulkyWeb.Utility
{
    public class OrderVM
    {
        public OrderHeader OrderHeader { get; set; }
        public OrderDetail OrderDetail { get; set; }
        public IEnumerable<OrderDetail> OrderDetailList { get; set; }
    }
}
