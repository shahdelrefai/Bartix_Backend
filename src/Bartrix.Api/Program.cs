using Bartrix.Api.Seeding;
using Bartrix.Api.DependencyInjection;
using Bartrix.Api.Hubs;
using Bartrix.Api.Media;
using Bartrix.Modules.Admin.Api;
using Bartrix.Modules.Auth.Api;
using Bartrix.BuildingBlocks.Persistence;
using Bartrix.Modules.Categories.Api;
using Bartrix.Modules.Delivery.Api;
using Bartrix.Modules.Listings.Api;
using Bartrix.Modules.Messaging.Api;
using Bartrix.Modules.Notifications.Api;
using Bartrix.Modules.Payments.Api;
using Bartrix.Modules.Profiles.Api;
using Bartrix.Modules.Reports.Api;
using Bartrix.Modules.Reputation.Api;
using Bartrix.Modules.Search.Api;
using Bartrix.Modules.Services.Api;
using Bartrix.Modules.Trades.Api;
using Bartrix.Modules.Wallet.Api;
using Bartrix.Modules.Withdrawals.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBartrixPlatform(builder.Configuration);
builder.Services.Configure<HostOptions>(o =>
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

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
app.MapMediaEndpoints();
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapListingsEndpoints();
app.MapTradesEndpoints();
app.MapMessagingEndpoints();
app.MapReputationEndpoints();
app.MapDeliveryEndpoints();
app.MapServicesEndpoints();
app.MapSearchEndpoints();
app.MapNotificationsEndpoints();
app.MapReportsEndpoints();
app.MapCategoriesEndpoints();
app.MapWalletEndpoints();
app.MapWithdrawalsEndpoints();
app.MapAdminEndpoints();
app.MapPaymentsEndpoints();
app.MapHub<TradeChatHub>("/hubs/trade-chat");

if (app.Environment.IsDevelopment())
    app.MapSeedEndpoints();
app.MapGet("/", () => Results.Ok(new
{
    Service = "Bartrix.Api",
    Status = "Running"
}));

app.Run();
