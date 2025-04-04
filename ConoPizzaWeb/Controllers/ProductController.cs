using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConoPizzaWeb.Data;
using ConoPizzaWeb.Models.Products;
using ConoPizzaWeb.Models.Cart;
using System.Threading.Tasks;
using ConoPizzaWeb.Extensions;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ConoPizzaWeb.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "Cart";

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product/Index/5?editItemId=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
        [HttpGet]
        public async Task<IActionResult> Index(int id, Guid? editItemId = null)
        {
            var product = await _context.Products
                .Include(p => p.Prices)
                .Include(p => p.ExtraOptions)
                .Include(p => p.Ingredients)
                    .ThenInclude(pi => pi.Ingredient)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // If editing, load the cart item and pass details via ViewBag
            if (editItemId.HasValue)
            {
                var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey) ?? new Cart();
                var itemToEdit = cart.Items.FirstOrDefault(ci => ci.CartItemId == editItemId.Value);
                if (itemToEdit != null)
                {
                    ViewBag.EditItemId = itemToEdit.CartItemId;
                    ViewBag.EditQuantity = itemToEdit.Quantity;

                    // Convert product.Prices to a List so we can use FindIndex
                    var pricesList = product.Prices?.ToList() ?? new List<ProductPrice>();
                    int foundIndex = pricesList.FindIndex(p => p.Size == itemToEdit.SelectedSize);
                    ViewBag.EditSizeIndex = foundIndex >= 0 ? foundIndex : 0;

                    // Pass extras (list of CartItemExtra)
                    ViewBag.EditExtras = itemToEdit.Extras;
                }
            }

            return View(product);
        }

        // POST: Product/AddToCart
        // Modified to accept an optional EditItemId so that if editing,
        // the old item is removed before adding the new one.
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            // Check if request is null
            if (request == null)
                return BadRequest("Request body is null.");


            // Copy the productId into a local variable so EF sees a simple variable
            int productId = request.ProductId;

            // Now use the local variable in the query
            var product = await _context.Products
                .Include(p => p.Prices)
                .Include(p => p.ExtraOptions)
                .FirstOrDefaultAsync(p => p.ProductId == productId);


            if (product == null)
                return NotFound();

            // Retrieve or create cart from session
            var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey) ?? new Cart();

            // If editing, remove the old item from the cart
            if (request.EditItemId.HasValue)
            {
                var oldItem = cart.Items.FirstOrDefault(i => i.CartItemId == request.EditItemId.Value);
                if (oldItem != null)
                {
                    cart.Items.Remove(oldItem);
                }
            }

            // Determine base price from selected size
            decimal basePrice = 0m;
            if (product.Prices != null && product.Prices.Count > 0)
            {
                if (request.SizeIndex >= 0 && request.SizeIndex < product.Prices.Count)
                {
                    basePrice = product.Prices[request.SizeIndex].Price;
                }
                else
                {
                    basePrice = product.Prices.First().Price;
                }
            }

            // Build extras list from request
            var cartItemExtras = new List<CartItemExtra>();
            if (request.Extras != null && request.Extras.Any())
            {
                foreach (var ex in request.Extras)
                {
                    cartItemExtras.Add(new CartItemExtra
                    {
                        ExtraName = ex.Name,
                        Price = ex.Price
                    });
                }
            }

            // Check if an identical item exists (same product, size, extras)
            var existingItem = cart.Items.FirstOrDefault(i =>
                 i.ProductId == product.ProductId &&
                 i.SelectedSize == product.Prices[request.SizeIndex].Size &&
                 HaveSameExtras(i.Extras, cartItemExtras)
            );

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    Title = product.Title,
                    ImageUrl = product.ImageUrl,
                    BasePrice = basePrice,
                    SelectedSize = product.Prices[request.SizeIndex].Size,
                    Extras = cartItemExtras,
                    Quantity = request.Quantity
                    // CartItemId auto-sets (e.g., via Guid.NewGuid() in the CartItem constructor)
                });
            }

            // Save updated cart in session
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);

            return Json(new { success = true, totalUniqueItems = cart.TotalUniqueItems });
        }

        // Helper method to compare extras (by name and price)
        private bool HaveSameExtras(List<CartItemExtra> existingExtras, List<CartItemExtra> newExtras)
        {
            if (existingExtras.Count != newExtras.Count) return false;
            foreach (var ex in newExtras)
            {
                if (!existingExtras.Any(e => e.ExtraName == ex.ExtraName && e.Price == ex.Price))
                    return false;
            }
            return true;
        }

        // GET: Product/Cart – displays the cart view.
        [HttpGet]
        public IActionResult Cart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey) ?? new Cart();
            return View(cart);
        }

        // POST: Product/RemoveFromCart – removes a product from the cart.
        [HttpPost]
        public IActionResult RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey) ?? new Cart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (item != null)
            {
                cart.Items.Remove(item);
                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }

            return Json(new
            {
                success = true,
                totalItems = cart.TotalUniqueItems,
                totalPrice = cart.Total
            });
        }


    }

    // Request model for adding to cart. Added optional EditItemId.
    public class AddToCartRequest
    {
        public Guid? EditItemId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int SizeIndex { get; set; }
        public List<SelectedExtraDto> Extras { get; set; } = new();
    }

    public class SelectedExtraDto
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    // Request model for removing an item from the cart.
    public class RemoveFromCartRequest
    {
        public int ProductId { get; set; }
    }
}
