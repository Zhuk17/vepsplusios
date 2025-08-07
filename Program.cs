using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using VepsPlusApi.Models;
using System.Text.Json; // !!! ВОТ ЭТОТ USING НЕОБХОДИМО ДОБАВИТЬ !!!
using System.Text.Json.Serialization; // Этот using для JsonPropertyName (если используется напрямую в моделях, а не в настройках)

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ИСПРАВЛЕНИЕ: Настраиваем Json-сериализацию/десериализацию
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Это сделает десериализацию входящих JSON нечувствительной к регистру.
        // Т.е., "username" или "Username" будут маппиться на свойство Username в C# классе.
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // Опционально: Если вы хотите, чтобы исходящие JSON (ответы от сервера)
        // использовали camelCase (стандарт для JSON), можно добавить:
        // options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        // Но если вы это сделаете, вам, возможно, придется обновить LoginResponse на клиенте
        // для ожиданий camelCase (userId, username, role, message).
        // Пока оставим PropertyNamingPolicy по умолчанию (PascalCase для исходящих)
        // чтобы избежать дальнейших расхождений с клиентской моделью ответа.
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VepsPlusApi", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

