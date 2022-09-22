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
}));

app.MapControllers();

app.Run();
