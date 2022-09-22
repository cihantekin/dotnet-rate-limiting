using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseRateLimiter(new RateLimiterOptions
{
    OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Lease.GetAllMetadata().ToList().ForEach(x =>
        {
            app.Logger.LogError(message: x.Key, x.Value);
        });
        return new ValueTask();
    },
    RejectionStatusCode = StatusCodes.Status429TooManyRequests
}.AddConcurrencyLimiter("controller", options =>
{
    options.QueueLimit = 1;
    options.PermitLimit = 1;
    options.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;

// Takes 3 requests and then wait 10 secons to refresh tokens. 
}).AddTokenBucketLimiter("tokenlimit", options =>
{
    options.QueueLimit = 0;
    options.TokenLimit = 3;
    options.AutoReplenishment = true;
    options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
    options.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
    options.TokensPerPeriod = 3;
}));

app.MapGet("/testtokenlimit", context =>
{
    context.Response.StatusCode = StatusCodes.Status200OK;
    return context.Response.WriteAsync("Hey you!");
}).RequireRateLimiting("tokenlimit");

app.MapControllers().RequireRateLimiting("controller");

app.Run();
