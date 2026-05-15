using Bartrix.Api.DependencyInjection;
using Bartrix.Api.Hubs;
using Bartrix.Modules.Auth.Api;
using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Delivery.Api;
using Bartrix.Modules.Listings.Api;
using Bartrix.Modules.Messaging.Api;
using Bartrix.Modules.Profiles.Api;
using Bartrix.Modules.Reputation.Api;
using Bartrix.Modules.Search.Api;
using Bartrix.Modules.Services.Api;
using Bartrix.Modules.Trades.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBartrixPlatform(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializers = scope.ServiceProvider.GetServices<IDatabaseInitializer>();

    foreach (var initializer in initializers)
    {
        await initializer.InitializeAsync();
    }
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapListingsEndpoints();
app.MapTradesEndpoints();
app.MapMessagingEndpoints();
app.MapReputationEndpoints();
app.MapDeliveryEndpoints();
app.MapServicesEndpoints();
app.MapSearchEndpoints();
app.MapHub<TradeChatHub>("/hubs/trade-chat");
app.MapGet("/", () => Results.Ok(new
{
    Service = "Bartrix.Api",
    Status = "Infrastructure scaffold ready"
}));

app.Run();
