using Sohba.Infrastructure.DBInitializer;

namespace Sohba.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task InitializeDatabaseAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();

            var initializer = scope.ServiceProvider
                .GetRequiredService<IDBInitializer>();

            await initializer.InitializeAsync();
        }
    }
}
