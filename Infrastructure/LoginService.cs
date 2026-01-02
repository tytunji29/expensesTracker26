using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using expensesTracker26.Application.Requests;
using expensesTracker26.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

public interface ILoginService
{
    Task<ReturnObject> LoginAsync(AppUserRequest request);
    Task<ReturnObject> RegisterAsync(AppUserRequest request);
}

public class LoginService : ILoginService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public LoginService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<ReturnObject> RegisterAsync(AppUserRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new Exception("Email already exists");

        var user = new AppUser
        {
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new ReturnObject
        {
            Status = true,
            Message = "User registered successfully",
            Data = GenerateToken(user)
        };

    }
    public async Task<ReturnObject> LoginAsync(AppUserRequest request)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var userId = user.Id;

        var totalExpenses = await _db.Expenses
            .AsNoTracking()
            .Where(e => e.CreatedBy == userId)
            .SumAsync(e => e.Amount);

        var totalIncomes = await _db.IncomeSources
            .AsNoTracking()
            .Where(i => i.CreatedBy == userId)
            .SumAsync(i => i.Amount);

        var token = GenerateToken(user);

        return new ReturnObject
        {
            Status = true,
            Message = "Login successful",
            Data = new
            {
                token,
                totalExpenses,
                totalIncomes,
                user.Email
            }
        };
    }


    private string GenerateToken(AppUser user)
    {
        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim("userId", user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_config["Jwt:ExpiryMinutes"]!)
            ),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
