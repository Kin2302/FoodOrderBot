using FoodOrderBot.Application.Auth;
using FoodOrderBot.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FoodOrderBot.Infrastructure.Facebook
{
    public class FacebookAuthService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<FacebookAuthService> logger) : IFacebookAuthService
    {
        private const string GraphApiVersion = "v21.0";


        public async Task<string> ExchangeForLongLivedTokenAsync(string shortLivedToken, CancellationToken ct = default)
        {
            var appId = config["Facebook:AppId"];
            var appSecret = config["Facebook:AppSecret"];

            var url = $"https://graph.facebook.com/{GraphApiVersion}/oauth/access_token" +
                      $"?grant_type=fb_exchange_token" +
                      $"&client_id={appId}" +
                      $"&client_secret={appSecret}" +
                      $"&fb_exchange_token={shortLivedToken}";

            var response = await httpClient.GetAsync(url, ct);


            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Facebook token exchange failed: {StatusCode} — {Body}",
                    response.StatusCode, errorBody);
                throw new HttpRequestException(
                    $"Facebook token exchange failed: {response.StatusCode} — {errorBody}");
            }

            string responseContent = await response.Content.ReadAsStringAsync(ct);

            using JsonDocument doc = JsonDocument.Parse(responseContent);
            JsonElement root = doc.RootElement;


            return root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Access token not found in response.");

        }

        public async Task<List<FacebookPageDto>> GetManagedPagesAsync(string userAccessToken, CancellationToken ct = default)
        {

            var url = $"https://graph.facebook.com/v21.0/me/accounts?fields=id,name,picture,access_token&access_token={userAccessToken}";

            var response = await httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Facebook token exchange failed: {StatusCode} — {Body}",
                    response.StatusCode, errorBody);
                throw new HttpRequestException(
                    $"Get managed pages failed: {response.StatusCode} — {errorBody}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = JsonSerializer.Deserialize<FacebookPagesResponse>(responseContent, options);

            return json?.Data.Select(page => new FacebookPageDto
            {
                PageId = page.Id,
                PageName = page.Name,
                PictureUrl = page.Picture?.Data?.Url,
                PageAccessToken = page.access_token
            }).ToList() ?? new List<FacebookPageDto>();
        }

        private class FacebookPagesResponse
        {
            public List<FacebookPageJson> Data { get; set; } = new();

        }

        private class FacebookPageJson
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string access_token { get; set; } = string.Empty;
            public FacebookPictureJson Picture { get; set; } = new();
        }

        private class FacebookPictureJson
        {
            public FacebookPictureDataJson Data { get; set; } = new();
        }
        private class FacebookPictureDataJson
        {
            public string Url { get; set; } = string.Empty;
        }
    }
}
