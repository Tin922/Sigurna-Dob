using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<SigurnaDobDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var appOrigin = builder.Configuration["Cors:AppOrigin"]
                ?? "https://localhost:7096";

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorApp", policy =>
        policy.WithOrigins(appOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("BlazorApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
