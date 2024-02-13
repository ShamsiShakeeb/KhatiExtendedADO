using Microsoft.Extensions.DependencyInjection;

namespace KhatiExtendedADO
{
    public static class ExtendedAdoResolver
    {
        public static IServiceCollection AdoDependency(this IServiceCollection services)
        {
            services.AddSingleton<IAdoProperties, AdoProperties>();
            return services;
        }
    }
}
