using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Perception;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string[] urls = { "http://localhost:5173", "http://127.0.0.1:5173", "http://172.19.20.183", "http://121.41.200.206:5173", "http://9oj.fun:5173" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCors",
        policy =>
        {
            policy.WithOrigins(urls)
                .SetIsOriginAllowedToAllowWildcardSubdomains()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
builder.Services.Configure<FormOptions>(options =>
{
    options.KeyLengthLimit = int.MaxValue;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});
builder.Services.AddDbContext<PerceptionContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddControllers();
//builder.Services.AddHostedService<PredictionService>();
builder.Services.AddSingleton<PredictionService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<ReApplyOptionalRouteParameterOperationFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();

app.Run();
