using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace kadroff.Components
{
    public class SalesforceService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public SalesforceService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        private async Task<(string AccessToken, string InstanceUrl)> GetAccessTokenAsync()
        {
            var values = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _config["Salesforce:ClientId"]! },
            { "client_secret", _config["Salesforce:ClientSecret"]! },
            //{ "username", _config["Salesforce:Username"]! },
            //{ "password", $"{_config["Salesforce:Password"]}{_config["Salesforce:SecurityToken"]}" }
        };

            var content = new FormUrlEncodedContent(values);    
            var response = await _httpClient.PostAsync("https://orgfarm-af9c64c183.my.salesforce.com/services/oauth2/token", content);

            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Error: {response.StatusCode}, {json}");
            }
            using var doc = JsonDocument.Parse(json);

            string token = doc.RootElement.GetProperty("access_token").GetString()!;
            string instanceUrl = doc.RootElement.GetProperty("instance_url").GetString()!;

            return (token, instanceUrl);
        }

        public async Task<bool> CreateAccountAndContactAsync(string companyName, string firstName, string lastName, string email, string phone)
        {
            var (token, instanceUrl) = await GetAccessTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var accountData = new { Name = companyName };
            var accountResponse = await _httpClient.PostAsJsonAsync($"{instanceUrl}/services/data/v60.0/sobjects/Account/", accountData);
            if (!accountResponse.IsSuccessStatusCode)
            {
                var errorString = await accountResponse.Content.ReadAsStringAsync();
                throw new Exception($"Error: {accountResponse.StatusCode}, {errorString}");
            }

            var accountJson = await accountResponse.Content.ReadAsStringAsync();
            using var accountDoc = JsonDocument.Parse(accountJson);
            string accountId = accountDoc.RootElement.GetProperty("id").GetString()!;

            var contactData = new
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                AccountId = accountId
            };

            var contactResponse = await _httpClient.PostAsJsonAsync($"{instanceUrl}/services/data/v60.0/sobjects/Contact/", contactData);
            return contactResponse.IsSuccessStatusCode;
        }
    }
}
