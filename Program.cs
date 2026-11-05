var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// ตั้งค่า CORS ให้อนุญาต Next.js (http://localhost:3000) สามารถเรียก API ได้
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins(
                        "http://localhost:3000",
                        "http://localhost:3001",
                        "http://localhost:5173",
                        "http://localhost:8000"
                        )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// เปิดใช้งาน Swagger ใน Development Environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ใช้งาน CORS Policy
app.UseCors("AllowNextJs");

app.UseAuthorization();

// แมป Route ของ Controller ทั้งหมดเข้าสู่ Routing System ของ ASP.NET Core
app.MapControllers();

app.Run();
