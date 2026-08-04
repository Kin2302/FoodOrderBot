namespace FoodOrderBot.Application.Auth;



public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }

    public static AuthResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    public static AuthResult Ok(string token, DateTime expiresAt) => new() { Success = true, Token = token, ExpiresAt = expiresAt };
}
public class FacebookAuthRequest 
{
    public string UserAccessToken { get; set; } = string.Empty;
}

public class FacebookPageDto 
{ 
    public string PageId { get; set; } = string.Empty; 
    public string PageName { get; set; } = string.Empty; 
    public string? PictureUrl { get; set; } = string.Empty; 
    public string PageAccessToken { get; set; } = string.Empty; 
}
public class FacebookPageResponse
{
    public string PageId { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string? PictureUrl { get; set; } = string.Empty;

}

public class SelectPageRequest 
{ 
    public string PageId { get; set; } = string.Empty; 
    public string PageAccessToken { get; set; } = string.Empty; 
    public string PageName { get; set; } = string.Empty; 
}