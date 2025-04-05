using ConoPizzaWeb.Models.Categories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConoPizzaWeb.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAdminUser(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Check if Admin role exists, create if not
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Check if admin user exists
            const string adminEmail = "admin@conopizza.com";
            const string adminUsername = "admin";
            const string adminPassword = "Admin123!"; // Set a strong password

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // Create the admin user
                adminUser = new IdentityUser
                {
                    UserName = adminUsername,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    // Add user to Admin role
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    // Log the creation (optional)
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Admin user created successfully. Username: {Username}", adminUsername);
                }
                else
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError("Failed to create admin user: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        public static async Task SeedCategories(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            // Check if categories already exist
            if (await dbContext.Categories.AnyAsync())
            {
                logger.LogInformation("Categories already seeded.");
                return;
            }

            // Define categories
            var categories = new List<Category>
            {
                new Category
                {
                    Name = "Pizza",
                    Description = "All Pizza Items",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Burgers",
                    Description = "All Burger Items",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Sandwiches",
                    Description = "All Sandwich Items",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            // Add categories to the database
            await dbContext.Categories.AddRangeAsync(categories);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Categories seeded successfully. Added {Count} categories.", categories.Count);
        }
    }
}