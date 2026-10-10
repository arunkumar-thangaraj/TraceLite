using Microsoft.EntityFrameworkCore;

namespace TraceLite.Sample;

public class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public double Total { get; set; }
}

public class ShopDbContext : DbContext
{
    public ShopDbContext(DbContextOptions<ShopDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        await db.Database.EnsureCreatedAsync();

        if (await db.Customers.AnyAsync())
        {
            return;
        }

        var customers = Enumerable.Range(1, 5)
            .Select(id => new Customer { Id = id, Name = $"Customer {id}" })
            .ToList();

        db.Customers.AddRange(customers);

        db.Orders.AddRange(Enumerable.Range(1, 20)
            .Select(id => new Order
            {
                Id = id,
                CustomerId = customers[id % customers.Count].Id,
                Total = id * 12.5
            }));

        await db.SaveChangesAsync();
    }
}
