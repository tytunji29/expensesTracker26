using expensesTracker26.Application.Requests;
using expensesTracker26.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddSwaggerGen();

// Read connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// App services
builder.Services.AddScoped<IFinanceService, FinanceService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ===== REST endpoints =====

app.MapPost("/api/incomes", async (IncomeSourceRequest income, IFinanceService service) =>
{
    await service.AddIncomeSource(income);
    return Results.Ok();
});

app.MapPost("/api/expenses", async (ExpenseRequest expense, IFinanceService service) =>
{
    await service.AddExpense(expense);
    return Results.Ok();
});


app.MapPost("/api/expenses-bulk", async (List<ExpenseRequest> expenses, IFinanceService service) =>
{
    await service.AddExpense(expenses);
    return Results.Ok();
});

app.MapPost("/api/bills", async (BillsHolderRequest billsHolder, IFinanceService service) =>
{
    await service.AddBillsHolder(billsHolder);
    return Results.Ok();
});

app.MapGet("/api/incomes", async (IFinanceService service) =>
{
    return Results.Ok(await service.GetIncomeSourcesAsync());
});

app.MapGet("/api/expenses", async (IFinanceService service) =>
{
    return Results.Ok(await service.GetExpensesAsync());
});

app.MapPost("/api/bills/link",
    async (BillsHolderRequest bills, IFinanceService service) =>
{
    await service.LinkIncomeToExpense(bills);
    return Results.Ok();
});

// ===== gRPC =====
app.MapGrpcService<FinanceGrpcService>();

app.Run();
