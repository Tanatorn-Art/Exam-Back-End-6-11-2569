var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// ดึงรายการ AllowedOrigins จาก Configuration (appsettings.json หรือ Environment Variable)
var defaultOrigins = new[]
{
    "http://localhost:3000",
    "http://localhost:3001",
    "http://localhost:5173",
    "https://exam.unknowsteps.company"
};
var configuredOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? defaultOrigins;

// ตั้งค่า CORS Policy ให้อนุญาตทั้ง Localhost และ Production Domain (https://exam.unknowsteps.company)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(configuredOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // รองรับ cookies / authorization headers หากจำเป็น
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// เปิดใช้งาน Swagger UI สำหรับทั้ง Local Dev และ Production (https://api-exam.unknowsteps.company/swagger)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Toyota Exam API v1");
    c.RoutePrefix = "swagger"; // เข้าถึงผ่าน /swagger หรือ /swagger/index.html
});

// ใช้งาน CORS Policy
app.UseCors("AllowFrontend");

app.UseAuthorization();

// แมป Route ของ Controller ทั้งหมด
app.MapControllers();

app.Run();
