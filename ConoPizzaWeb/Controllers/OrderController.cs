using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConoPizzaWeb.Data;
using ConoPizzaWeb.Models.Orders;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;      
using ConoPizzaWeb.Hubs;

namespace ConoPizzaWeb.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<OrdersHub> _hubContext; // 1️⃣ Inject the hub context
        public OrderController(ApplicationDbContext context, IHubContext<OrdersHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // POST: /Order/CreateOrder
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid order data");
            }

            // Build the OrderItems from the view model items.
            var orderItems = new List<OrderItem>();
            if (model.Items != null && model.Items.Any())
            {
                foreach (var item in model.Items)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        SelectedSize = item.SelectedSize, // <--- store it
                        SelectedExtras = item.Extras != null
                       ? item.Extras.Select(extra => new OrderItemExtra
                        {
                            ExtraName = extra.ExtraName,
                            ExtraPrice = extra.ExtraPrice
                        }).ToList()
                        : new List<OrderItemExtra>()
                    };
                    orderItems.Add(orderItem);
                }
            }

            // Create a new order using your Order model.
            var order = new Order
            {
                ConsumerName = model.Name,
                Address = model.Address,
                PhoneNumber = model.Phone,
                Total = model.Total,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Konfirmo,
                OrderItems = orderItems,
                CreatedAt = DateTime.UtcNow,  // ✅ Set CreatedAt
                UpdatedAt = DateTime.UtcNow   // ✅ Set UpdatedAt
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Optionally, clear the cart from session.
            HttpContext.Session.Remove("Cart");

            // Inside CreateOrder(), after you do _context.SaveChangesAsync():
            var createdOrder = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SelectedExtras)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
            // 3️⃣ Broadcast the "new order" event to all admin clients listening
            //    with a simplified payload. 
            await _hubContext.Clients.All.SendAsync("ReceiveOrder", new
            {
                orderId = createdOrder.OrderId,
                consumerName = createdOrder.ConsumerName,
                phoneNumber = createdOrder.PhoneNumber,
                address = createdOrder.Address,
                total = createdOrder.Total,
                status = createdOrder.Status.ToString(),
                orderDate = createdOrder.OrderDate,
                // Flatten the OrderItems:
                orderItems = createdOrder.OrderItems.Select(oi => new {
                    productTitle = oi.Product?.Title,
                    selectedSize = oi.SelectedSize,
                    unitPrice = oi.UnitPrice,
                    quantity = oi.Quantity,
                    selectedExtras = oi.SelectedExtras.Select(x => new {
                        extraName = x.ExtraName,
                        extraPrice = x.ExtraPrice
                    }).ToList()
                }).ToList()
            });

            // Return JSON with success and the new OrderId.
            return Json(new { success = true, orderId = order.OrderId });
        }

        // GET: /Order/Checkout?orderId=123
        [HttpGet]
        public async Task<IActionResult> Checkout(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SelectedExtras)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order); // Pass the Order model to the Checkout view
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatus(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }
            return Json(new { status = order.Status.ToString() });
        }
    }

    // ViewModel for order creation.
    public class OrderViewModel
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public decimal Total { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        // Snapshot of product price at ordering time.
        public decimal UnitPrice { get; set; }

        public string SelectedSize { get; set; }  // e.g. "Vogël", "Mesme", etc.
        public List<OrderItemExtraViewModel> Extras { get; set; } = new List<OrderItemExtraViewModel>();
    }

    public class OrderItemExtraViewModel
    {
        public string ExtraName { get; set; }
        public decimal ExtraPrice { get; set; }
    }
}
