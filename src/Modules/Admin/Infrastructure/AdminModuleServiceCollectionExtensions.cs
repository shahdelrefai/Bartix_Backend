using Bartrix.Modules.Admin.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Bartrix.Modules.Admin.Infrastructure;

public static class AdminModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services)
    {
        services.AddScoped<IAdminService, AdminService>();
        return services;
    }
}
