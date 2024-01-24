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

app.UseHttpsRedirection();

app.MapPost("/create", async (User user, IDistributedCache cache) =>
{
    var json = JsonSerializer.Serialize(user);
    byte[] encodedUser = Encoding.UTF8.GetBytes(json);

    await cache.SetAsync("user", encodedUser, new DistributedCacheEntryOptions()
    {
        SlidingExpiration = TimeSpan.FromSeconds(20)
    });

    return Results.Ok();
})
.WithName("CreateUser")
.WithOpenApi();

app.Run();

record User(string Name, string Email);
