using FreelanceAI.Core.Interfaces;
using FreelanceAI.Core.Models;
using FreelanceAI.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceAI.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJsonFileService(this IServiceCollection services, 
        Action<JsonFileServiceOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<JsonFileServiceOptions>(options => { }); // Use defaults
        }

        services.AddSingleton<IJsonFileService, JsonFileService>();
        return services;
    }
}