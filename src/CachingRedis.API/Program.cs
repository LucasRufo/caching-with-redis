using CachingRedis.API;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ChachingRedis:";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var cacheKey = "users";

app.MapGet("/users/cache-aside", async (IDistributedCache cache) => {

    var usersFromCacheByte = await cache.GetAsync(cacheKey);

    //check if userlist was already on cache
    if(usersFromCacheByte is not null && usersFromCacheByte.Length != 0)
    {
        //in case they were, we can already return
        var users = JsonSerializer.Deserialize<List<User>>(usersFromCacheByte);

        return Results.Ok(users);
    }

    //if they are not, we need to go to our data source and update the cache
    var usersFromDb = await Database.GetUsers();

    //don't cache empty values
    if(usersFromDb.Count != 0)
    {
        var json = JsonSerializer.Serialize(usersFromDb);
        var bytes = Encoding.UTF8.GetBytes(json);

        await cache.SetAsync(cacheKey, bytes, new DistributedCacheEntryOptions()
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        });
    }

    return Results.Ok(usersFromDb);
});

app.MapPost("/users/write-through", async (User user, IDistributedCache cache) =>
{
    //insert to main datastore
    await Database.InsertUser(user);

    var users = await Database.GetUsers();

    var json = JsonSerializer.Serialize(users);
    var bytes = Encoding.UTF8.GetBytes(json);

    //and insert to cache right after
    //questions:
    //what happens if my cache provider is offline?
    //my main and cache source can be out of sync?
    await cache.SetAsync(cacheKey, bytes, new DistributedCacheEntryOptions()
    {
        SlidingExpiration = TimeSpan.FromMinutes(5)
    });

    return Results.Ok();
});


//Endpoint just to insert users into a list
app.MapPost("/users", async (User user) =>
{
    await Database.InsertUser(user);

    return Results.Ok();
});

app.Run();

