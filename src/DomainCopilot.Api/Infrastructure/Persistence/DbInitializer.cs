using System;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DomainCopilot.Api.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await context.Database.MigrateAsync();

        string[] roles = { "Admin", "HiringManager", "Recruiter" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await SeedUserAsync(userManager, "admin@copilot.local", "Admin@123456", "System Admin", "IT", "Admin");
        await SeedUserAsync(userManager, "manager@copilot.local", "Manager@123456", "Hiring Manager", "Engineering", "HiringManager");
        await SeedUserAsync(userManager, "recruiter@copilot.local", "Recruiter@123456", "HR Recruiter", "HR", "Recruiter");
    }

    private static async Task SeedUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string fullName, string department, string role)
    {
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                Department = department,
                EmailConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
