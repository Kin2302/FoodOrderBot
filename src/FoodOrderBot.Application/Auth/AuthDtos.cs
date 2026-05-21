namespace FoodOrderBot.Application.Auth;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }

    public static AuthResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    public static AuthResult Ok(string token, DateTime expiresAt) => new() { Success = true, Token = token, ExpiresAt = expiresAt };
}
