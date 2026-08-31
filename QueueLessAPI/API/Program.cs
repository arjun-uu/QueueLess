using API.Extensions;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddSwaggerServices();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseSwaggerServices();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();