using System.Security.Claims;

public static class HttpContextExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
        => int.Parse(user.FindFirst("userId")!.Value);
}
