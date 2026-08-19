var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
