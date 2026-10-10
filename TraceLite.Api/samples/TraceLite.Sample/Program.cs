using Microsoft.EntityFrameworkCore;
using TraceLite;
using TraceLite.Sample;

var builder = WebApplication.CreateBuilder(args);

var traceLiteConnectionString =
    builder.Configuration.GetConnectionString("TraceLite")
    ?? throw new InvalidOperationException(
        "Set ConnectionStrings:TraceLite to the PostgreSQL database that stores the traces.");

// 1. Register TraceLite and its trace store.
//    Every HttpClient from IHttpClientFactory is traced from here on.
builder.Services.AddTraceLite(traceLiteConnectionString);

// 2. Let TraceLite record your own DbContext's queries (any EF Core provider).
builder.Services.AddDbContext<ShopDbContext>((serviceProvider, options) =>
    options
        .UseSqlite("Data Source=shop.db")
        .AddTraceLiteInterceptor(serviceProvider));

builder.Services.AddHttpClient("Profiles", client =>
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/"));

var app = builder.Build();

// 3. Trace every request.
app.UseTraceLite();

await app.Services.MigrateTraceLiteDatabaseAsync();

await ShopDbContext.SeedAsync(app.Services);

// Deliberate N+1: one customer query per order, which shows up
// in the TraceLite timeline as a staircase of SQL spans.
app.MapGet("/orders", async (ShopDbContext db) =>
{
    var orders = await db.Orders
        .AsNoTracking()
        .OrderBy(order => order.Id)
        .Take(10)
        .ToListAsync();

    var result = new List<object>();

    foreach (var order in orders)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .FirstAsync(customer => customer.Id == order.CustomerId);

        result.Add(new { order.Id, order.Total, Customer = customer.Name });
    }

    return result;
});

// The same data in a single query, to compare with /orders.
app.MapGet("/orders/fast", async (ShopDbContext db) =>
    await db.Orders
        .AsNoTracking()
        .OrderBy(order => order.Id)
        .Take(10)
        .Select(order => new
        {
            order.Id,
            order.Total,
            Customer = order.Customer!.Name
        })
        .ToListAsync());

// A database query followed by an outbound HTTP call.
app.MapGet("/customers/{id:int}/profile", async (
    int id,
    ShopDbContext db,
    IHttpClientFactory httpClientFactory) =>
{
    var customer = await db.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(customer => customer.Id == id);

    if (customer is null)
    {
        return Results.NotFound();
    }

    var profile = await httpClientFactory
        .CreateClient("Profiles")
        .GetStringAsync($"users/{id}");

    return Results.Text(profile, "application/json");
});

app.Run();
