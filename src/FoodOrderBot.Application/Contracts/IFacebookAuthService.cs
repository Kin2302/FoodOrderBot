using FoodOrderBot.Application.Auth;

namespace FoodOrderBot.Application.Contracts
{
    public interface IFacebookAuthService
    {
        Task<string> ExchangeForLongLivedTokenAsync(string shortLivedToken, CancellationToken ct = default);
        Task<List<FacebookPageDto>> GetManagedPagesAsync(string userAccessToken, CancellationToken ct = default);
    }
}
