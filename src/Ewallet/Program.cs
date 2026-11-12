using Ewallet.Data;
using Ewallet.Exceptions;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

app.Run();