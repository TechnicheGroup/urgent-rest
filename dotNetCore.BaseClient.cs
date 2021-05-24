using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TestHarness
{
    public class BaseClient
    {
        private readonly HttpClient client;

        private readonly string baseUrl;

        private readonly string apiServiceMarker;

        private readonly string apiToken;

        private readonly string apiSecret;

        protected BaseClient(string baseUrl, string apiServiceMarker, string apiToken, string apiSecret)
        {
            this.baseUrl = baseUrl;
            this.apiServiceMarker = apiServiceMarker;
            this.apiToken = apiToken;
            this.apiSecret = apiSecret;

            this.client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        protected async Task<KeyValuePair<HttpStatusCode, T>> GetAsync<T>(string url)
        {
            var cts = new CancellationToken();

            this.AddHeaders(url, HttpMethod.Get);

            var responseMessage = await this.client.GetAsync(url, cts);

            return await this.ParseResponse<T>(responseMessage);
        }

        protected async Task<KeyValuePair<HttpStatusCode, T>> PostAsync<T>(object content, string url)
        {
            this.AddHeaders(url, HttpMethod.Post);

            var serializedContent = content == null ? null : JsonSerializer.Serialize(content);

            var responseMessage = await this.client.PostAsync(url, new StringContent(serializedContent, Encoding.UTF8, "application/json"));

            return await this.ParseResponse<T>(responseMessage);
        }

        protected async Task<KeyValuePair<HttpStatusCode, T>> PutAsync<T>(object content, string url)
        {
            this.AddHeaders(url, HttpMethod.Put);

            var serializedContent = content == null ? null : JsonSerializer.Serialize(content);

            var responseMessage = await this.client.PutAsync(url, new StringContent(serializedContent, Encoding.UTF8, "application/json"));

            return await this.ParseResponse<T>(responseMessage);
        }

        protected async Task<KeyValuePair<HttpStatusCode, T>> DeleteAsync<T>(string url)
        {
            this.AddHeaders(url, HttpMethod.Delete);

            var responseMessage = await this.client.DeleteAsync(url);

            return await this.ParseResponse<T>(responseMessage);
        }

        private async Task<KeyValuePair<HttpStatusCode, T>> ParseResponse<T>(HttpResponseMessage responseMessage)
        {
            try
            {
                if (responseMessage == null)
                {
                    return new KeyValuePair<HttpStatusCode, T>(HttpStatusCode.BadRequest, default);
                }

                var responseData = await responseMessage.Content.ReadAsStringAsync();

                if (responseMessage.StatusCode == HttpStatusCode.BadRequest && !string.IsNullOrEmpty(responseData))
                {
                    return new KeyValuePair<HttpStatusCode, T>(responseMessage.StatusCode, JsonSerializer.Deserialize<T>(responseData));
                }

                if (!responseMessage.IsSuccessStatusCode || responseMessage.Content == null)
                {
                    return new KeyValuePair<HttpStatusCode, T>(responseMessage.StatusCode, default);
                }

                if (string.IsNullOrEmpty(responseData))
                {
                    return new KeyValuePair<HttpStatusCode, T>(responseMessage.StatusCode, default);
                }

                return new KeyValuePair<HttpStatusCode, T>(responseMessage.StatusCode, JsonSerializer.Deserialize<T>(responseData));
            }
            catch
            {
                return new KeyValuePair<HttpStatusCode, T>(responseMessage.StatusCode, default);
            }
        }

        private void AddHeaders(string methodUrl, HttpMethod httpMethod)
        {
            var absoluteUri = new Uri(new Uri(this.baseUrl), methodUrl);

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var digest = this.CalculateDigest(absoluteUri, httpMethod, timestamp);

            var authHeader = $"{this.apiToken}:{digest}";

            this.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(this.apiServiceMarker, authHeader);

            if (this.client.DefaultRequestHeaders.Contains("X-Timestamp"))
            {
                this.client.DefaultRequestHeaders.Remove("X-Timestamp");
                this.client.DefaultRequestHeaders.Add("X-Timestamp", timestamp);
            }
            else
            {
                this.client.DefaultRequestHeaders.Add("X-Timestamp", timestamp);
            }
        }

        private string CalculateDigest(Uri absoluteUri, HttpMethod httpMethod, string timeStamp)
        {
            var method = httpMethod.ToString().ToUpper();
            var relativeUri = absoluteUri.AbsolutePath.ToLower();

            var plainText = $"{method}+{relativeUri}+{timeStamp}";

            string digest;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(this.apiSecret)))
            {
                digest = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(plainText)));
            }

            return digest;
        }

        protected void Dispose()
        {
            this.client.Dispose();
        }
    }
}
