using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi;

using expensesTracker26.Application.Requests;
using expensesTracker26.Infrastructure;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


builder.WebHost.ConfigureKestrel(options =>
{
    // gRPC requires HTTP/2
    options.ListenLocalhost(5080, o =>
    {
        o.Protocols = HttpProtocols.Http2; // gRPC
    });

    // REST/Swagger on all network interfaces
    options.Listen(System.Net.IPAddress.Any, 5079, o =>
    {
        o.Protocols = HttpProtocols.Http1; // REST + Swagger
    });
});
// REST + gRPC
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {token}"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    // Apply to all operations
    c.OperationFilter<AddBearerAuthHeaderOperationFilter>();
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
}).RequireAuthorization().WithTags("Income");

app.MapPost("/api/IncomeSourceForTheMonth", async (IncomeSourceForTheMonthRequest income, IFinanceService service) =>
{
    await service.AddIncomeSourceForTheMonth(income);
    return Results.Ok();
}).RequireAuthorization().WithTags("IncomeForTheMonth");

app.MapPost("/api/update-incomesource", async (UpdateById<IncomeSourceRequest> income, IFinanceService service) =>
{
    await service.UpdateIncomeSource(income);
    return Results.Ok();
}).RequireAuthorization().WithTags("Income");


// app.MapPost("/api/bills", async (BillsHolderRequest billsHolder, IFinanceService service) =>
// {
//     await service.AddBillsHolder(billsHolder);
//     return Results.Ok();
// }).RequireAuthorization().WithTags("Bills");

app.MapPost("/api/bills-list", async (List<BillsHolderRequest> billsHolder, IFinanceService service) =>
{
    var res = await service.AddBillsHolder(billsHolder);
    return Results.Ok(res);
}).RequireAuthorization().WithTags("Bills");
app.MapPost("/api/register", async (AppUserRequest billsHolder, ILoginService service) =>
{
    var ret = await service.RegisterAsync(billsHolder);
    return Results.Ok(ret);
}).WithTags("Authentication");

app.MapPost("/api/login", async (AppUserRequest billsHolder, ILoginService service) =>
{
    var ret = await service.LoginAsync(billsHolder);
    return Results.Ok(ret);
}).WithTags("Authentication");
app.MapGet("/api/incomes", async (IFinanceService service) =>
{
    var ret = await service.GetIncomeSourcesAsync();
    return Results.Ok(ret);
}).RequireAuthorization().WithTags("Income");
app.MapGet("/api/income-sources/balances", async (IFinanceService service) =>
{
    var ret = await service.GetIncomeSourcesBalanceAsync();
    return Results.Ok(ret);
}).RequireAuthorization().WithTags("Income");

app.MapGet("/api/bills/unpaid-bills", async (IFinanceService service) =>
{
    var ret = await service.GetUnpaidBills();
    return Results.Ok(ret);
}).RequireAuthorization().WithTags("Bills");
app.MapGet("/api/bills/allbills", async (IFinanceService service) =>
{
    var ret = await service.GetAllBillsForTheMonth();
    return Results.Ok(ret);
}).RequireAuthorization().WithTags("Bills");

app.MapGet("/api/bills/paid-bills-forthemonth/{monthId}/{year}", async (int monthId, int year, IFinanceService service) =>
{
    var ret = await service.GetPaidBillsForTheMonth(monthId, year);
    return Results.Ok(ret);
}).RequireAuthorization().WithTags("Bills");

app.MapGet("/api/bills/unpaid-bills-forthemonth/{monthId}/{year}", async (int monthId, int year, IFinanceService service) =>
{
    var ret = await service.GetUnPaidBillsForTheMonth(monthId, year);
    return Results.Ok(ret);
}).RequireAuthorization().WithTags("Bills");

app.MapGet("/api/unplanned-balance", async (IFinanceService service) =>
{
    var ret = await service.GetTotalBalanceAsync();
    return Results.Ok(ret);
}).RequireAuthorization().WithTags("Bills");

app.MapPost("/api/bills/paid-bill/{billid}", async (int billid, BillsHolderRequest bill, IFinanceService service) =>
{
    await service.EditBillsHolder(bill, billid);
    return Results.Ok();
}).RequireAuthorization().WithTags("Bills");

app.MapPost("/api/bills/flag-bill/{billid}", async (int billid, bool isPaid, IFinanceService service) =>
{
    await service.FlagIsPaid(billid, isPaid);
    return Results.Ok();
}).RequireAuthorization().WithTags("Bills");
// app.MapGet("/api/expenses", async (IFinanceService service) =>
// {
//     var ret = await service.GetExpensesAsync();
//     return Results.Ok(ret);
// }).RequireAuthorization().WithTags("Expenses");



// ===== gRPC =====
//app.MapGrpcService<FinanceGrpcService>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
