using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ConoPizzaWeb.Data;
using OfficeOpenXml;
using DinkToPdf;
using DinkToPdf.Contracts;
using ScottPlot;          // v4 API
using System.Drawing;     // for System.Drawing.Color
using System.Drawing.Imaging;
using D2DOrientation = DinkToPdf.Orientation; // disambiguate from ScottPlot.Orientation
using Microsoft.EntityFrameworkCore;
using ConoPizzaWeb.Models.Products;
using X.PagedList.Extensions;
using ConoPizzaWeb.Models.ViewModels;
using ConoPizzaWeb.Models.Ingredients;
using Microsoft.AspNetCore.Mvc.Rendering;
using ConoPizzaWeb.Models.Orders;
using System.Text;

namespace ConoPizzaWeb.Controllers
{
    [Authorize(Roles = "Admin")] // Default for the controller
    public class AdminController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConverter _pdfConverter;

        public AdminController(SignInManager<IdentityUser> signInManager, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IConverter pdfConverter)
        {
            _signInManager = signInManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _pdfConverter = pdfConverter;
        }

        // This action requires the user to be authenticated as Admin.
        public IActionResult Index()
        {

            return View();
        }

        // Allow anonymous access to the login page.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Allow anonymous access to process the login.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Please enter both username and password.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Redirect to the returnUrl if provided and local; otherwise, redirect to the admin dashboard.
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Admin");
                }
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminMenuDashboard(
    int pageProd = 1,
    int pageSizeProd = 10,
    int pageIng = 1,
    int pageSizeIng = 10,
    int? filterCategoryId = null,
    bool? filterIsFeatured = null,
    string searchTerm = null)
        {
            // 1) Start with your products query
            IQueryable<Product> productsQuery = _context.Products
     .Include(p => p.Category)
     .Include(p => p.Prices)
     .Include(p => p.ExtraOptions)
     .Include(p => p.Ingredients)
         .ThenInclude(pi => pi.Ingredient)
     .OrderBy(p => p.ProductId);

            //2)Filter
            if (filterCategoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == filterCategoryId.Value);
            }
            if (filterIsFeatured.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.IsFeatured == filterIsFeatured.Value);
            }
            if (!string.IsNullOrEmpty(searchTerm))
            {
                productsQuery = productsQuery.Where(p => p.Title.Contains(searchTerm));
            }

            // 3) Execute the query
            var productList = await productsQuery.ToListAsync();

            // 4) Get the full list of ingredients (unchanged)
            var ingredientList = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync();

            // 5) Convert to paged lists
            var pagedProducts = productList.ToPagedList(pageProd, pageSizeProd);
            var pagedIngredients = ingredientList.ToPagedList(pageIng, pageSizeIng);

            // 6) (Optional) Filter featured products for your popular section
            var featuredProducts = productList.Where(p => p.IsFeatured).ToList();

            // 7) Build your ViewModel (note: add the filter parameters so they persist in the view)
            var vm = new AdminMenuDashboardViewModel
            {
                Products = pagedProducts,
                Ingredients = pagedIngredients,
                FeaturedProducts = featuredProducts,
                FilterCategoryId = filterCategoryId,
                FilterIsFeatured = filterIsFeatured,
                SearchTerm = searchTerm
            };

            // Optionally, pass categories to the view via ViewBag for the filter form
            ViewBag.Categories = _context.Categories.ToList();

            return View(vm);
        }


        // Example GET for Edit
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Prices)
                .Include(p => p.ExtraOptions)
                .Include(p => p.Ingredients)
                    .ThenInclude(pi => pi.Ingredient)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            // Build the edit ViewModel
            var vm = new ProductEditViewModel
            {
                ProductId = product.ProductId,
                Title = product.Title,
                Description = product.Description,
                CategoryId = product.CategoryId,
                IsFeatured = product.IsFeatured,
                ExistingImageUrl = product.ImageUrl
                // We'll fill PriceSmall/Medium/Large or PriceSingle below
            };

            // If product.Prices has exactly 3, we treat it as a pizza => fill PriceSmall/Medium/Large
            if (product.Prices != null && product.Prices.Count == 3)
            {
                vm.PriceSmall = product.Prices.FirstOrDefault(x => x.Size == "Vogël")?.Price;
                vm.PriceMedium = product.Prices.FirstOrDefault(x => x.Size == "Mesme")?.Price;
                vm.PriceLarge = product.Prices.FirstOrDefault(x => x.Size == "Madhe")?.Price;
            }
            else
            {
                // Single price scenario
                vm.PriceSingle = product.Prices?.FirstOrDefault()?.Price;
            }

            // Map extras
            vm.Extras = product.ExtraOptions
                .Select(e => new ProductExtraViewModel
                {
                    Name = e.Name,
                    Price = e.Price
                })
                .ToList();

            // Map selected ingredient IDs
            vm.SelectedIngredientIds = product.Ingredients
                .Select(pi => pi.IngredientId)
                .ToList();

            // Build a JSON list of preselected ingredients (id, name, img) so we can show real names in the chips
            var preselectedIngDetails = product.Ingredients
                .Select(pi => new
                {
                    id = pi.IngredientId,
                    name = pi.Ingredient.Name,
                    img = pi.Ingredient.InImgUrl
                })
                .ToList();

            // Convert that to JSON
            ViewBag.PreselectedIngredientsJson = System.Text.Json.JsonSerializer.Serialize(preselectedIngDetails);

            // Also populate categories, ingredients for the dropdown
            ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "Name", product.CategoryId);
            ViewBag.Ingredients = _context.Ingredients.ToList();

            return View(vm);
        }



        // Example POST for Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductEditViewModel model)
        {
       
            // 1) Load the existing product
            var product = await _context.Products
                .Include(p => p.Prices)
                .Include(p => p.ExtraOptions)
                .Include(p => p.Ingredients)
                .FirstOrDefaultAsync(p => p.ProductId == model.ProductId);

            if (product == null)
                return NotFound();

            // 2) Update basic fields
            product.Title = model.Title;
            product.Description = model.Description;
            product.CategoryId = model.CategoryId;
            product.IsFeatured = model.IsFeatured;

            // 3) If a new image was uploaded, remove the old file if it exists, then set new one
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                // Remove old file if present
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var oldFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        product.ImageUrl.TrimStart('/')
                    );
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Upload the new file
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }
                product.ImageUrl = Path.Combine("/uploads/products", uniqueFileName).Replace("\\", "/");
            }
            // else if user didn't upload a new file => keep the old one

            // 4) Update prices
            // We'll remove the old ProductPrices and re-add new ones from the form
            product.Prices.Clear();

            // Check if category is Pizza => expect 3 prices
            var category = await _context.Categories.FindAsync(model.CategoryId);
            if (category != null && category.Name.ToLower() == "pizza")
            {
                if (model.PriceSmall.HasValue && model.PriceMedium.HasValue && model.PriceLarge.HasValue)
                {
                    product.Prices.Add(new ProductPrice { Size = "Vogël", Price = model.PriceSmall.Value });
                    product.Prices.Add(new ProductPrice { Size = "Mesme", Price = model.PriceMedium.Value });
                    product.Prices.Add(new ProductPrice { Size = "Madhe", Price = model.PriceLarge.Value });
                }
                else
                {
                    ModelState.AddModelError("", "Please provide all price values for Pizza sizes.");
                    ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "Name", model.CategoryId);
                    ViewBag.Ingredients = _context.Ingredients.ToList();
                    return View(model);
                }
            }
            else
            {
                // Single price
                if (model.PriceSingle.HasValue)
                {
                    product.Prices.Add(new ProductPrice { Size = "Regular", Price = model.PriceSingle.Value });
                }
                else
                {
                    ModelState.AddModelError("", "Please provide a price for the product.");
                    ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "Name", model.CategoryId);
                    ViewBag.Ingredients = _context.Ingredients.ToList();
                    return View(model);
                }
            }

            // 5) Update extras
            // Clear old extras
            product.ExtraOptions.Clear();
            // Re-add from form
            if (model.Extras != null && model.Extras.Any())
            {
                foreach (var extra in model.Extras)
                {
                    if (!string.IsNullOrWhiteSpace(extra.Name))
                    {
                        product.ExtraOptions.Add(new ProductExtra
                        {
                            Name = extra.Name,
                            Price = extra.Price
                        });
                    }
                }
            }

            // 6) Update ingredients
            // Remove old ones
            product.Ingredients.Clear();
            // Add new
            if (model.SelectedIngredientIds != null && model.SelectedIngredientIds.Any())
            {
                foreach (var ingredientId in model.SelectedIngredientIds)
                {
                    product.Ingredients.Add(new ProductIngredient { IngredientId = ingredientId });
                }
            }

            // 7) Save changes
            _context.Update(product);
            await _context.SaveChangesAsync();

            TempData["success"] = "Product updated successfully!";
            return RedirectToAction(nameof(AdminMenuDashboard));
        }


        // Example POST for Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // 1) delete file from folder if exists
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var oldFilePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    product.ImageUrl.TrimStart('/')
                );
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // 2) remove from DB
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["success"] = "Product deleted successfully!";
            return RedirectToAction(nameof(AdminMenuDashboard));
        }

        [HttpGet]
        public IActionResult CreateProduct()
        {
            // Populate categories and ingredients for dropdowns
            ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "Name");
            ViewBag.Ingredients = _context.Ingredients.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdowns before returning view
                ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "Name");
                ViewBag.Ingredients = _context.Ingredients.ToList();
                return View(model);
            }

            // Create a new Product entity
            var product = new Product
            {
                Title = model.Title,
                Description = model.Description,
                CategoryId = model.CategoryId,
                IsFeatured = model.IsFeatured,
                // ImageUrl will be set after processing file upload
            };

            // Process image upload if one was provided
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }
                product.ImageUrl = Path.Combine("/uploads/products", uniqueFileName).Replace("\\", "/");
            }

            // Determine pricing based on category (assumes category name "Pizza" for pizza products)
            var category = await _context.Categories.FindAsync(model.CategoryId);
            if (category != null && category.Name.ToLower() == "pizza")
            {
                // Expect three price inputs for pizza
                if (model.PriceSmall.HasValue && model.PriceMedium.HasValue && model.PriceLarge.HasValue)
                {
                    product.Prices.Add(new ProductPrice { Size = "Vogël", Price = model.PriceSmall.Value });
                    product.Prices.Add(new ProductPrice { Size = "Mesme", Price = model.PriceMedium.Value });
                    product.Prices.Add(new ProductPrice { Size = "Madhe", Price = model.PriceLarge.Value });
                }
                else
                {
                    ModelState.AddModelError("", "Please provide all price values for Pizza sizes.");
                    ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "Name");
                    ViewBag.Ingredients = new SelectList(_context.Ingredients.ToList(), "IngredientId", "Name");
                    return View(model);
                }
            }
            else
            {
                // Single price for non-Pizza products
                if (model.PriceSingle.HasValue)
                {
                    product.Prices.Add(new ProductPrice { Size = "Regular", Price = model.PriceSingle.Value });
                }
                else
                {
                    ModelState.AddModelError("", "Please provide a price for the product.");
                    ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "Name");
                    ViewBag.Ingredients = new SelectList(_context.Ingredients.ToList(), "IngredientId", "Name");
                    return View(model);
                }
            }

            // Process extras (if any)
            if (model.Extras != null && model.Extras.Any())
            {
                foreach (var extra in model.Extras)
                {
                    if (!string.IsNullOrWhiteSpace(extra.Name))
                    {
                        product.ExtraOptions.Add(new ProductExtra
                        {
                            Name = extra.Name,
                            Price = extra.Price
                        });
                    }
                }
            }

            // Process selected ingredients (if any)
            if (model.SelectedIngredientIds != null && model.SelectedIngredientIds.Any())
            {
                foreach (var ingredientId in model.SelectedIngredientIds)
                {
                    product.Ingredients.Add(new ProductIngredient { IngredientId = ingredientId });
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["success"] = "New product created successfully!";
            return RedirectToAction(nameof(AdminMenuDashboard));
        }


        [HttpGet]
        public IActionResult CreateIngredient()
        {
            // Return a view to create an Ingredient (similar to CreateProduct)
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIngredient(IngredientViewModel vm)
        {

            // We'll create a new Ingredient entity to save to DB
            var newIngredient = new Ingredient
            {
                Name = vm.Name,
                Description = vm.Description
            };

            // 1) Check if a file was uploaded
            if (vm.UploadFile != null && vm.UploadFile.Length > 0)
            {
                // 2) Generate a unique file name (optional)
                var uniqueFileName = Guid.NewGuid().ToString()
                                     + "_"
                                     + vm.UploadFile.FileName;

                // 3) Decide where to store the file
                //    For example, "wwwroot/uploads/ingredients"
                //    Make sure the folder exists or create it.
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "ingredients"
                );

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 4) Build the file path
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 5) Copy the file to our folder
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.UploadFile.CopyToAsync(fileStream);
                }

                // 6) Store the relative path in InImgUrl
                // For example: "/uploads/ingredients/uniqueFile.jpg"
                newIngredient.InImgUrl = Path.Combine("/uploads/ingredients", uniqueFileName)
                    .Replace("\\", "/");
            }

            // Save to DB
            _context.Ingredients.Add(newIngredient);
            await _context.SaveChangesAsync();

            TempData["success"] = "New ingredient created!";
            return RedirectToAction(nameof(AdminMenuDashboard));
        }

        [HttpGet]
        public async Task<IActionResult> EditIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
                return NotFound();

            // Populate a ViewModel with existing data
            var vm = new IngredientViewModel
            {
                Id = ingredient.IngredientId, // keep track of ID
                Name = ingredient.Name,
                Description = ingredient.Description,
                // We do NOT set UploadFile here because that is for new uploads
                ExistingImagePath = ingredient.InImgUrl // store current path if we want to show a thumbnail or keep it
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditIngredient(IngredientViewModel vm)
        {

            // Load the ingredient from DB
            var ingredientToUpdate = await _context.Ingredients.FindAsync(vm.Id);
            if (ingredientToUpdate == null)
                return NotFound();

            // Update basic fields
            ingredientToUpdate.Name = vm.Name;
            ingredientToUpdate.Description = vm.Description;

            // If user uploaded a new file
            if (vm.UploadFile != null && vm.UploadFile.Length > 0)
            {
                // 1) remove old file from server if it exists
                if (!string.IsNullOrEmpty(ingredientToUpdate.InImgUrl))
                {
                    // build full path to the old image
                    var oldFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        ingredientToUpdate.InImgUrl.TrimStart('/')
                    );

                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // 2) generate unique name for new upload
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + vm.UploadFile.FileName;

                // 3) create or confirm folder
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "ingredients"
                );
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // 4) copy new file
                var newFilePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                {
                    await vm.UploadFile.CopyToAsync(fileStream);
                }

                // 5) update new path in DB
                ingredientToUpdate.InImgUrl = Path.Combine("/uploads/ingredients", uniqueFileName)
                                               .Replace("\\", "/");
            }
            else
            {
                // No new file => keep old InImgUrl as is
                // (No action needed, we do NOT overwrite the existing path)
            }

            // Save changes
            _context.Update(ingredientToUpdate);
            await _context.SaveChangesAsync();

            TempData["success"] = "Ingredient updated successfully!";
            return RedirectToAction(nameof(AdminMenuDashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
                return NotFound();

            // 1) delete file from folder if exists
            if (!string.IsNullOrEmpty(ingredient.InImgUrl))
            {
                var oldFilePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    ingredient.InImgUrl.TrimStart('/')
                );
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // 2) remove from DB
            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();

            TempData["success"] = "Ingredient deleted successfully!";
            return RedirectToAction(nameof(AdminMenuDashboard));
        }

        // GET: OrderDetails
        public async Task<IActionResult> OrderDetails(string startDate, string endDate, string status)
        {
            // Load orders with their items, product details, and extras
            var ordersQuery = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SelectedExtras)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            // Apply date filters
            if (DateTime.TryParse(startDate, out DateTime startDateParsed))
            {
                var startUtc = startDateParsed.Date.ToUniversalTime();
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= startUtc);
            }

            if (DateTime.TryParse(endDate, out DateTime endDateParsed))
            {
                var endUtc = endDateParsed.Date.AddDays(1).ToUniversalTime();
                ordersQuery = ordersQuery.Where(o => o.OrderDate < endUtc);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out OrderStatus statusParsed))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == statusParsed);
            }

            var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();

            // Pass selected filters to the view
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.SelectedStatus = status;

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateModel model)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            if (Enum.TryParse<OrderStatus>(model.status, out var newStatus))
            {
                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;  // ✅ Ensure `UpdatedAt` is updated

                await _context.SaveChangesAsync();
                return Json(new { success = true, newStatus = order.Status.ToString() });
            }
            return Json(new { success = false });
        }

        // POST: /AdminOrder/CancelOrder/5
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = OrderStatus.Anuluar;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Json(new { success = true, newStatus = order.Status.ToString() });
        }

        public async Task<IActionResult> Statuset(string status)
        {
            // Handle special case for delayed orders
            if (status?.ToLower() == "vonuara")
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-40);

                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.SelectedExtras)
                    .Where(o => o.OrderDate <= cutoffTime &&
                                o.Status != OrderStatus.Anuluar &&
                                o.Status != OrderStatus.Arritur &&
                                o.Status != OrderStatus.Paguar)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                ViewBag.Status = "Porosit E Vonuara (Më shumë se 40 minuta)";
                return View(orders);
            }

            // Existing status filter handling
            if (Enum.TryParse<OrderStatus>(status, out OrderStatus statusFilter))
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.SelectedExtras)
                    .Where(o => o.Status == statusFilter)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                ViewBag.Status = statusFilter.ToString();
                return View(orders);
            }

            return NotFound();
        }
        public class StatusUpdateModel
        {
            public string status { get; set; } = string.Empty;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageFeedbacks(
    int page = 1,
    int pageSize = 10,
    string searchTerm = null,
    int? ratingFilter = null,
    string sortOrder = "date_desc")
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.DateSortParam = sortOrder == "date_asc" ? "date_desc" : "date_asc";
            ViewBag.RatingSortParam = sortOrder == "rating_asc" ? "rating_desc" : "rating_asc";
            ViewBag.CurrentFilter = searchTerm;
            ViewBag.RatingFilter = ratingFilter;

            var feedbacks = _context.Feedbacks.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                feedbacks = feedbacks.Where(f =>
                    f.Emri.Contains(searchTerm) ||
                    f.Email.Contains(searchTerm) ||
                    f.Subject.Contains(searchTerm) ||
                    f.Mesazhi.Contains(searchTerm));
            }

            if (ratingFilter.HasValue)
            {
                feedbacks = feedbacks.Where(f => f.RatingStars == ratingFilter.Value);
            }

            // Apply sorting
            feedbacks = sortOrder switch
            {
                "date_asc" => feedbacks.OrderBy(f => f.CreatedAt),
                "rating_asc" => feedbacks.OrderBy(f => f.RatingStars),
                "rating_desc" => feedbacks.OrderByDescending(f => f.RatingStars),
                _ => feedbacks.OrderByDescending(f => f.CreatedAt),
            };

            // Use ToListAsync() to execute the query asynchronously
            var feedbackList = await feedbacks.ToListAsync();

            // Convert to paged list
            var pagedFeedbacks = feedbackList.ToPagedList(page, pageSize);

            return View(pagedFeedbacks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            try
            {
                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Feedback deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting feedback";
            }

            return RedirectToAction(nameof(ManageFeedbacks));
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Qarkullimet(string timeRange = "today")
        {
            // 1) Figure out date range
            DateTime now = DateTime.UtcNow;
            DateTime startDate;
            DateTime endDate = now;

            switch (timeRange?.ToLower())
            {
                case "today":
                    startDate = now.Date;
                    break;
                case "week":
                    startDate = now.AddDays(-7);
                    break;
                case "month":
                    startDate = now.AddMonths(-1);
                    break;
                case "year":
                    startDate = now.AddYears(-1);
                    break;
                default:
                    startDate = now.Date; // default to 'today'
                    break;
            }

            // 2) Query only orders with status Arritur or Paguar in the date range
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o =>
                    (o.Status == OrderStatus.Arritur || o.Status == OrderStatus.Paguar) &&
                    o.OrderDate >= startDate &&
                    o.OrderDate <= endDate
                )
                .ToListAsync();

            // --------------------------------------------------------
            // A) REVENUE CHART (line)
            // --------------------------------------------------------
            var dailyRevenues = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new {
                    Day = g.Key,
                    Total = g.Sum(x => x.Total)  // or compute from OrderItems if needed
                })
                .OrderBy(x => x.Day)
                .ToList();

            var revenueLabels = dailyRevenues
                .Select(dr => dr.Day.ToString("MMM dd"))
                .ToList();

            var revenueData = dailyRevenues
                .Select(dr => dr.Total)
                .ToList();

            ViewBag.LabelsJson = System.Text.Json.JsonSerializer.Serialize(revenueLabels);
            ViewBag.DataJson = System.Text.Json.JsonSerializer.Serialize(revenueData);

            // --------------------------------------------------------
            // B) ORDER TREND CHART (bar)
            // --------------------------------------------------------
            // Example grouping by time of day.
            int breakfastCount = 0;
            int lunchCount = 0;
            int dinnerCount = 0;
            int lateNightCount = 0;

            foreach (var order in orders)
            {
                var hour = order.OrderDate.Hour;
                // Adjust logic to your local time if needed
                if (hour >= 5 && hour < 11)
                    breakfastCount++;
                else if (hour >= 11 && hour < 15)
                    lunchCount++;
                else if (hour >= 17 && hour < 21)
                    dinnerCount++;
                else
                    lateNightCount++;
            }

            var orderTrendLabels = new List<string> { "Breakfast", "Lunch", "Dinner", "Late Night" };
            var orderTrendData = new List<int> { breakfastCount, lunchCount, dinnerCount, lateNightCount };

            ViewBag.OrderTrendLabelsJson = System.Text.Json.JsonSerializer.Serialize(orderTrendLabels);
            ViewBag.OrderTrendDataJson = System.Text.Json.JsonSerializer.Serialize(orderTrendData);

            // --------------------------------------------------------
            // C) MAIN METRICS GRID
            // --------------------------------------------------------
            // 1) Total Revenue
            decimal totalRevenue = orders.Sum(o => o.Total);

            // 2) Completed Orders
            int completedOrders = orders.Count; // Arritur/Paguar in that range

            // Calculate Average Delivery Time (Performance)
            var deliveryTimes = orders
                .Where(o => o.UpdatedAt > o.OrderDate) // ✅ Ensure only valid updates are considered
                .Select(o => (o.UpdatedAt - o.OrderDate).TotalMinutes)
                .ToList();


            // ✅ Calculate both the Average and Fastest Delivery Time
            double avgDeliveryTime = deliveryTimes.Any() ? deliveryTimes.Average() : -1;
            double fastestDeliveryTime = deliveryTimes.Any() ? deliveryTimes.Min() : -1;


            // 3) Canceled Orders in the same date range
            // We'll check 'OrderStatus.Anuluar' in the same date range
            int canceledOrders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Anuluar
                    && o.OrderDate >= startDate
                    && o.OrderDate <= endDate)
                .CountAsync();

            // 4) Month-over-Month (or prior period) comparison
            // We'll do a simple approach: if timeRange=month => prior month, etc.
            // For simplicity, let's just do prior month. You can tailor as needed.
            var previousStart = startDate.AddMonths(-1);
            var previousEnd = startDate;

            var previousOrders = await _context.Orders
                .Where(o => (o.Status == OrderStatus.Arritur || o.Status == OrderStatus.Paguar)
                            && o.OrderDate >= previousStart
                            && o.OrderDate < previousEnd)
                .ToListAsync();

            decimal prevRevenueTotal = previousOrders.Sum(o => o.Total);
            decimal revenueChangePercentage = 0;
            if (prevRevenueTotal != 0)
            {
                revenueChangePercentage = (totalRevenue - prevRevenueTotal) / prevRevenueTotal * 100;
            }

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.CanceledOrders = canceledOrders;
            ViewBag.RevenueChangePercentage = revenueChangePercentage;

            // --------------------------------------------------------
            // E) PERFORMANCE METRICS
            // --------------------------------------------------------
            // E1) Top Selling Item (by quantity)
            var topItem = orders
                .SelectMany(o => o.OrderItems)
                .GroupBy(oi => oi.Product.Title)
                .Select(g => new {
                    ProductName = g.Key,
                    Quantity = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .FirstOrDefault();

            string topItemName = topItem?.ProductName ?? "N/A";
            int topItemQty = topItem?.Quantity ?? 0;

            // E2) Delivery Performance -> you don’t have DeliveredAt or similar
            // So let's use a placeholder, e.g. "N/A" or "No data"
            ViewBag.TopItemName = topItemName;
            ViewBag.TopItemQty = topItemQty;
            ViewBag.DeliveryPerformancePercent = avgDeliveryTime > 0 ? $"{Math.Round(avgDeliveryTime, 1)}m" : "N/A";
            ViewBag.FastestDelivery = fastestDeliveryTime >= 0 ? $"{Math.Round(fastestDeliveryTime, 1)}m" : "N/A";

            ViewBag.SelectedRange = timeRange;
            return View();
        }

        [HttpGet]
        public IActionResult GeneratePdfReport(string timeRange = "today")
        {
            // --- 1. Determine the Date Range (same as in Qarkullimet) ---
            DateTime now = DateTime.UtcNow;
            DateTime startDate;
            DateTime endDate = now;

            switch (timeRange?.ToLower())
            {
                case "today":
                    startDate = now.Date;
                    break;
                case "week":
                    startDate = now.AddDays(-7);
                    break;
                case "month":
                    startDate = now.AddMonths(-1);
                    break;
                case "year":
                    startDate = now.AddYears(-1);
                    break;
                default:
                    startDate = now.Date;
                    break;
            }

            // --- 2. Query Orders Using the Same Filters ---
            var orders = _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => (o.Status == OrderStatus.Arritur || o.Status == OrderStatus.Paguar|| o.Status == OrderStatus.Anuluar)
                            && o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToList();

            decimal totalRevenue = orders.Sum(o => o.Total);
            int completedOrders = orders.Count;
            int canceledOrders = _context.Orders.Count(o => o.Status == OrderStatus.Anuluar);

            // --- 3. Delivery Metrics ---
            var deliveryTimes = orders
                .Where(o => o.UpdatedAt > o.OrderDate)
                .Select(o => (o.UpdatedAt - o.OrderDate).TotalMinutes)
                .ToList();
            double avgDeliveryTime = deliveryTimes.Any() ? deliveryTimes.Average() : -1;
            double fastestDeliveryTime = deliveryTimes.Any() ? deliveryTimes.Min() : -1;

            // --- 4. Top Selling Item ---
            var topItem = orders
                .SelectMany(o => o.OrderItems)
                .GroupBy(oi => oi.Product.Title)
                .Select(g => new { ProductName = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .FirstOrDefault();
            string topItemName = topItem?.ProductName ?? "N/A";
            int topItemQty = topItem?.Quantity ?? 0;

            // --- 5. Prepare Chart Data ---
            // A) Daily Revenue (Line Chart)
            var dailyRevenues = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Day = g.Key, Total = g.Sum(x => x.Total) })
                .OrderBy(x => x.Day)
                .ToList();
            double[] xs = dailyRevenues.Select((dr, i) => (double)i).ToArray();
            double[] ys = dailyRevenues.Select(dr => (double)dr.Total).ToArray();
            string[] dayLabels = dailyRevenues.Select(dr => dr.Day.ToString("MMM dd")).ToArray();

            // --- Create the Line Chart using ScottPlot 4.x ---
            var pltLine = new ScottPlot.Plot(450, 300);
            pltLine.Title("Daily Revenue");
            pltLine.XLabel("Day Index");
            pltLine.YLabel("Revenue (€)");
            pltLine.AddScatter(xs, ys, color: Color.MediumBlue, lineWidth: 2);
            pltLine.XTicks(xs, dayLabels);
            byte[] lineChartPng = pltLine.GetImageBytes();

            // B) Time-of-Day Distribution (Bar Chart)
            int breakfastCount = 0, lunchCount = 0, dinnerCount = 0, lateNightCount = 0;
            foreach (var o in orders)
            {
                int hour = o.OrderDate.Hour;
                if (hour >= 5 && hour < 11) breakfastCount++;
                else if (hour >= 11 && hour < 15) lunchCount++;
                else if (hour >= 17 && hour < 21) dinnerCount++;
                else lateNightCount++;
            }
            double[] barValues = { breakfastCount, lunchCount, dinnerCount, lateNightCount };
            string[] barLabels = { "Breakfast", "Lunch", "Dinner", "Late Night" };

            var pltBar = new ScottPlot.Plot(450, 300);
            pltBar.Title("Orders by Time of Day");
            pltBar.YLabel("Number of Orders");
            var barSeries = pltBar.AddBar(barValues);
            barSeries.FillColor = Color.MediumPurple;
            barSeries.BorderColor = Color.Black;
            barSeries.BorderLineWidth = 1;
            pltBar.XTicks(new double[] { 0, 1, 2, 3 }, barLabels);
            pltBar.SetAxisLimits(yMin: 0);
            byte[] barChartPng = pltBar.GetImageBytes();

            // --- 6. Build the HTML String ---
            var sb = new StringBuilder();
            sb.Append(@"
<html>
<head>
    <meta charset='UTF-8'/>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #3498db; color: white; }
        h2 { text-align: center; }
        .stats { margin-top: 20px; padding: 10px; border-radius: 5px; background-color: #f8f9fa; }
        .charts-row { width: 100%; }
        .chart-cell { width: 50%; vertical-align: top; padding: 10px; }
    </style>
</head>
<body>
    <h2>Food Delivery Dashboard Report</h2>
    <div class='stats'>
        <p><strong>Total Revenue:</strong> " + totalRevenue + @" €</p>
        <p><strong>Completed Orders:</strong> " + completedOrders + @"</p>
        <p><strong>Canceled Orders:</strong> " + canceledOrders + @"</p>
        <p><strong>Average Delivery Time:</strong> " + (avgDeliveryTime >= 0 ? Math.Round(avgDeliveryTime, 1) + " min" : "N/A") + @"</p>
        <p><strong>Fastest Delivery Time:</strong> " + (fastestDeliveryTime >= 0 ? Math.Round(fastestDeliveryTime, 1) + " min" : "N/A") + @"</p>
        <p><strong>Top Selling Item:</strong> " + topItemName + " (Sold " + topItemQty + @" times)</p>
    </div>
    
    <h3>Charts</h3>
    <table style='width:100%; border:0; table-layout:fixed;'>
      <tr>
        <td class='chart-cell'>
          <p><strong>Daily Revenue Chart</strong></p>
          <img src='data:image/png;base64," + Convert.ToBase64String(lineChartPng) + @"' alt='Daily Revenue Chart' style='width:100%; height:auto;'/>
        </td>
        <td class='chart-cell'>
          <p><strong>Orders by Time of Day</strong></p>
          <img src='data:image/png;base64," + Convert.ToBase64String(barChartPng) + @"' alt='Orders by Time of Day Chart' style='width:100%; height:auto;'/>
        </td>
      </tr>
    </table>
    
    <h3>Orders List</h3>
    <table>
      <tr>
        <th>Order ID</th>
        <th>Customer</th>
        <th>Total (€)</th>
        <th>Date</th>
        <th>Status</th>
      </tr>");

            foreach (var order in orders)
            {
                var rowColor = "";
                if (order.Status == OrderStatus.Arritur) rowColor = "background-color:#d4edda;";
                if (order.Status == OrderStatus.Paguar) rowColor = "background-color:#cce5ff;";
                if (order.Status == OrderStatus.Anuluar) rowColor = "background-color:tomato;";

                sb.AppendFormat(@"
      <tr style='{5}'>
        <td>{0}</td>
        <td>{1}</td>
        <td>{2} €</td>
        <td>{3}</td>
        <td>{4}</td>
      </tr>",
                    order.OrderId,
                    order.ConsumerName,
                    order.Total,
                    order.OrderDate.ToString("yyyy-MM-dd HH:mm"),
                    order.Status,
                    rowColor
                );
            }

            sb.Append("</table></body></html>");

            // --- 7. CONVERT HTML TO PDF USING DinkToPdf ----------------------
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = DinkToPdf.ColorMode.Color,
                    Orientation = D2DOrientation.Portrait,
                    PaperSize = PaperKind.A4
                },
                Objects = {
            new ObjectSettings {
                HtmlContent = sb.ToString(),
                WebSettings = { DefaultEncoding = "utf-8" }
            }
        }
            };

            byte[] pdfBytes = _pdfConverter.Convert(doc);
            return File(pdfBytes, "application/pdf", "DashboardReport.pdf");
        }


        // ✅ EXPORT FULL DASHBOARD REPORT AS EXCEL
        [HttpGet]
        public IActionResult ExportReport(string timeRange = "today")
        {
            // --- 1. Determine the Date Range (same as in Qarkullimet) ---
            DateTime now = DateTime.UtcNow;
            DateTime startDate;
            DateTime endDate = now;

            switch (timeRange?.ToLower())
            {
                case "today":
                    startDate = now.Date;
                    break;
                case "week":
                    startDate = now.AddDays(-7);
                    break;
                case "month":
                    startDate = now.AddMonths(-1);
                    break;
                case "year":
                    startDate = now.AddYears(-1);
                    break;
                default:
                    startDate = now.Date;
                    break;
            }

            // 1) Grab your data
            var orders = _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => (o.Status == OrderStatus.Arritur || o.Status == OrderStatus.Paguar || o.Status == OrderStatus.Anuluar)
              && o.OrderDate >= startDate && o.OrderDate <= endDate)
              .ToList();

            // 2) EPPlus requires license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Dashboard Report");

                // HEADER styling
                worksheet.Cells["A1:E1"].Style.Font.Bold = true;
                worksheet.Cells["A1:E1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells["A1:E1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

                // HEADERS
                worksheet.Cells[1, 1].Value = "Order ID";
                worksheet.Cells[1, 2].Value = "Customer Name";
                worksheet.Cells[1, 3].Value = "Total (€)";
                worksheet.Cells[1, 4].Value = "Order Date";
                worksheet.Cells[1, 5].Value = "Status";

                int row = 2;
                foreach (var order in orders)
                {
                    worksheet.Cells[row, 1].Value = order.OrderId;
                    worksheet.Cells[row, 2].Value = order.ConsumerName;
                    worksheet.Cells[row, 3].Value = order.Total;   // numeric column
                    worksheet.Cells[row, 4].Value = order.OrderDate.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cells[row, 5].Value = order.Status.ToString();
                    row++;
                }

                // Format numeric column (Total) with currency or decimal
                worksheet.Column(3).Style.Numberformat.Format = "#,##0.00";
                worksheet.Column(3).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                // Auto-fit
                worksheet.Cells.AutoFitColumns();

                // 3) Return as file
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "DashboardReport.xlsx"
                );
            }
        }


        // Logout action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

    }
}
