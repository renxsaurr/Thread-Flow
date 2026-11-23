using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ThreadFlow.Infrastructure.data.Master;

namespace ThreadFlow.Api.Authentication;

public static class PlatformAdminBootstrapper
{
    public static async Task EnsureConfiguredAdminAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("PlatformAdminBootstrapper");
        var email = configuration["Bootstrap:PlatformAdminEmail"];
        var password = configuration["Bootstrap:PlatformAdminPassword"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
            return;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Set both Bootstrap:PlatformAdminEmail and Bootstrap:PlatformAdminPassword, " +
                "or leave both unset.");
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PlatformUser>>();
        if (userManager.Users.Any(user => user.IsPlatformAdmin))
        {
            logger.LogInformation("A platform administrator already exists; bootstrap skipped.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException(
                "The bootstrap email is already assigned to a non-platform user.");
        }

        var admin = new PlatformUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TenantId = null,
            IsPlatformAdmin = true
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not create the bootstrap platform administrator: {errors}");
        }

        logger.LogWarning("Created the initial platform administrator from bootstrap configuration.");
    }
}
