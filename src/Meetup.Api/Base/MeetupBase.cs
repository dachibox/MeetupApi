﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meetup.Api.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable once CheckNamespace
namespace Meetup.Api
{
    internal class MeetupBase
    {
        internal static string BASE_URL = "https://api.meetup.com";

        /// <summary>
        ///     Executes HttpClient query asynchronous.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryUrl">The query URL.</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <param name="content">The content.</param>
        /// <param name="method">The method .</param>
        /// <returns>Task&lt;T&gt;.</returns>
        internal static async Task<T> ExecuteQueryAsync<T>(StringBuilder queryUrl, CancellationToken cancellationToken,
            HttpContent content = null, HttpMethodTypes method = HttpMethodTypes.GET)
        {
            var authClient = new HttpClient {Timeout = TimeSpan.FromMilliseconds(30000)};
            HttpResponseMessage result;
            switch (method)
            {
                case HttpMethodTypes.GET:
                    result =
                        await
                            authClient.GetAsync(new Uri(queryUrl.ToString()), cancellationToken);
                    break;
                case HttpMethodTypes.POST:
                    result =
                        await
                            authClient.PostAsync(new Uri(queryUrl.ToString()), content, cancellationToken);
                    break;
                case HttpMethodTypes.PUT:
                    result =
                        await
                            authClient.PutAsync(new Uri(queryUrl.ToString()), content, cancellationToken);
                    break;
                case HttpMethodTypes.DELETE:
                    result =
                        await
                            authClient.DeleteAsync(new Uri(queryUrl.ToString()), cancellationToken);
                    break;
                default:
                    result = null;
                    break;
            }

            if (result != null && !result.StatusCode.ToString().ToLower().Equals("badgateway") &&
                !result.StatusCode.ToString().ToLower().Equals("badrequest") &&
                !result.StatusCode.ToString().ToLower().Equals("serviceunavailable"))
            {
                var data = await ProcessJson<T>(result.Content);
                return data;
            }

            return default(T);
        }

        public static async Task<bool> RenewAccessToken()
        {
            if (MeetupApi.OauthSettings == null)
                throw new ArgumentException("Initialize MeetupApi with your ClientId and ClientSecret from MeetupApi.ConfigureOauth");

            if (string.IsNullOrWhiteSpace(MeetupApi.TokenSettings.AccessToken))
                return false;

            if (DateTime.UtcNow < new DateTime(MeetupApi.TokenSettings.KeyValidUntil))
                return true;

            var queryUrl = new StringBuilder("https://secure.meetup.com/oauth2/access");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", MeetupApi.OauthSettings.ClientId),
                new KeyValuePair<string, string>("client_secret", MeetupApi.OauthSettings.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", MeetupApi.TokenSettings.RefreshToken),
            });

            var response = await ExecuteQueryAsync<TokenRoot>(queryUrl, CancellationToken.None, content, HttpMethodTypes.POST);

            if (response == null)
                throw new HttpRequestException(
                    "Ops! Something went wrong :S. Please try again, if the error persist contact with the developer to fix the issue.");

            MeetupApi.TokenSettings.AccessToken = response.AccessToken;
            var nextTime = DateTime.UtcNow.AddSeconds(response.ExpiresIn).Ticks;
            MeetupApi.TokenSettings.KeyValidUntil = nextTime;
            MeetupApi.TokenSettings.RefreshToken = response.RefreshToken;

            return true;
        }

        /// <summary>
        ///     Processes the json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content">The content.</param>
        /// <returns>Task&lt;T&gt;.</returns>
        private static async Task<T> ProcessJson<T>([NotNull] HttpContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            var json = await content.ReadAsStringAsync();
            if (json.StartsWith("<!DOCTYPE html>"))
            {
                return default(T);
            }

            var result = ProcessJson<T>(json);
            return result;
        }

        /// <summary>
        ///     Processes the json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content">The content.</param>
        /// <returns>T</returns>
        private static T ProcessJson<T>(string content)
        {
            if (string.IsNullOrEmpty(content)) throw new ArgumentException("Argument is null or empty", nameof(content));

            var deserializedData = JsonConvert.DeserializeObject<T>(content,
                new IsoDateTimeConverter {DateTimeFormat = "dd/MM/yyyy"});
            return deserializedData;
        }
    }
}