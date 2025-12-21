using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using expensesTracker26.Application.Requests;
using expensesTracker26.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ===== Configure Kestrel for gRPC (HTTP/2) =====
builder.WebHost.ConfigureKestrel(options =>
{
    // gRPC requires HTTP/2
    options.ListenLocalhost(5080, o =>
    {
        o.Protocols = HttpProtocols.Http2; // gRPC
    });

    // Optional: REST/Swagger on separate port (HTTP/1.1)
    options.ListenLocalhost(5079, o =>
    {
        o.Protocols = HttpProtocols.Http1; // REST + Swagger
    });
});

// REST + gRPC
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    // c.AddSecurityRequirement(new OpenApiSecurityRequirement
    // {
    //     {
    //         new OpenApiSecurityScheme
    //         {
    //             Reference = new OpenApiReference
    //             {
    //                 Type = ReferenceType.SecurityScheme,
    //                 Id = "Bearer"
    //             }
    //         },
    //         Array.Empty<string>()
    //     }
    // });

});


// Read connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddAuthorization();

// App services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<ILoginService, LoginService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ===== REST endpoints =====

app.MapPost("/api/incomes", async (IncomeSourceRequest income, IFinanceService service) =>
{
    await service.AddIncomeSource(income);
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/api/expenses", async (ExpenseRequest expense, IFinanceService service) =>
{
    await service.AddExpense(expense);
    return Results.Ok();
}).RequireAuthorization();


app.MapPost("/api/expenses-bulk", async (List<ExpenseRequest> expenses, IFinanceService service) =>
{
    await service.AddExpense(expenses);
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/api/bills", async (BillsHolderRequest billsHolder, IFinanceService service) =>
{
    await service.AddBillsHolder(billsHolder);
    return Results.Ok();
}).RequireAuthorization();
app.MapPost("/api/register", async (AppUserRequest billsHolder, ILoginService service) =>
{
    await service.RegisterAsync(billsHolder);
    return Results.Ok();
});

app.MapPost("/api/login", async (AppUserRequest billsHolder, ILoginService service) =>
{
    await service.LoginAsync(billsHolder);
    return Results.Ok();
});
app.MapGet("/api/incomes", async (IFinanceService service) =>
{
    return Results.Ok(await service.GetIncomeSourcesAsync());
}).RequireAuthorization();
app.MapGet("/api/bills/unpaid-bills", async (IFinanceService service) =>
{
    return Results.Ok(await service.GetUnpaidBills());
}).RequireAuthorization();

app.MapGet("/api/bills/paid-bills-forthemonth/{monthId}/{year}", async (int monthId, int year, IFinanceService service) =>
{
    return Results.Ok(await service.GetPaidBillsForTheMonth(monthId, year));
}).RequireAuthorization();

app.MapGet("/api/bills/unpaid-bills-forthemonth/{monthId}/{year}", async (int monthId, int year, IFinanceService service) =>
{
    return Results.Ok(await service.GetUnPaidBillsForTheMonth(monthId, year));
}).RequireAuthorization();


app.MapGet("/api/expenses", async (IFinanceService service) =>
{
    return Results.Ok(await service.GetExpensesAsync());
}).RequireAuthorization();



// ===== gRPC =====
app.MapGrpcService<FinanceGrpcService>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
