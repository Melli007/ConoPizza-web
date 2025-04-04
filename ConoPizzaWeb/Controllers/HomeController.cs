using ConoPizzaWeb.Data;
using ConoPizzaWeb.Hubs;
using ConoPizzaWeb.Models;
using ConoPizzaWeb.Models.Feedback;
using ConoPizzaWeb.Models.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ConoPizzaWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<FeedbackHub> _hubContext; // 1?? inject IHubContext<FeedbackHub>
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context,IHubContext<FeedbackHub> hubContext)
        {
            _logger = logger;
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            // Query for featured products (and include category and prices if needed)
            List<Product> featuredProducts = await _context.Products
                .Where(p => p.IsFeatured)
                .Include(p => p.Category)
                .Include(p => p.Prices)
                .ToListAsync();


            return View(featuredProducts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Feedback
        public IActionResult Feedback()
        {
            return View();
        }

        // POST: Feedback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Feedback([Bind("Emri,Email,Subject,Mesazhi,RatingStars")] Feedback feedback)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    feedback.CreatedAt = DateTime.Now; // Add timestamp
                    _context.Add(feedback);
                    await _context.SaveChangesAsync();

                    // 2?? Broadcast the new feedback to all connected admin clients
                    await _hubContext.Clients.All.SendAsync("ReceiveFeedback", new
                    {
                        FeedbackId = feedback.FeedbackId,
                        Emri = feedback.Emri,
                        Email = feedback.Email,
                        Subject = feedback.Subject,
                        Mesazhi = feedback.Mesazhi,
                        RatingStars = feedback.RatingStars,
                        CreatedAt = feedback.CreatedAt
                    });

                    TempData["SuccessMessage"] = "Thank you for your feedback!";
                    return RedirectToAction(nameof(Feedback));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving feedback");
                    ModelState.AddModelError("", "An error occurred while saving. Please try again.");
                }
            }

            // Return with validation errors
            return View(feedback);
        }

    }
}
