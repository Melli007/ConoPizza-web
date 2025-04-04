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
    }
}
