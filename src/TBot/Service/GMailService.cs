using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TBot.Models;

namespace TBot.Service
{
    public class AccTokenResponse
    {
        public bool success { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
    }

    public class GMailService : IGMailService
    {
        static string[] Scopes = { GmailService.Scope.GmailModify };
        static string ApplicationName = "[YOUR_GMAIL_APPLICATION_NAME]";
        private static readonly HttpClient client = new HttpClient();

        private readonly GmailService _service;
        private UserCredential _credential;

        private string CurrentRefreshToken { get; set; } = "[YOUR_INITIAL_REFRESH_TOKEN]";

        public bool ValidToken { get; set; }

        public GMailService()
        {
            var credential = CreateCredentialsAsync().Result;
            // Create Gmail API service.
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            _service = service;
        }



        public async Task<UserCredential> CreateCredentialsAsync()
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = "[YOUR_GMAIL_CLIENT_ID]",
                    ClientSecret = "[YOUR_GMAIL_CLIENT_SECRET]"
                },
                Scopes = Scopes,
                DataStore = new FileDataStore("Store")
            });

            var response = await client.PostAsJsonAsync("https://developers.google.com/oauthplayground/refreshAccessToken", new
            {
                client_id = "[YOUR_GMAIL_CLIENT_ID]",
                client_secret = "[YOUR_GMAIL_CLIENT_SECRET]",
                refresh_token = CurrentRefreshToken,
                token_uri = "https://oauth2.googleapis.com/token"
            });

            var accessTokenResponse = await response.Content.ReadAsAsync<AccTokenResponse>();

            if (accessTokenResponse.success)
            {
                CurrentRefreshToken = accessTokenResponse.refresh_token;

                var token = new TokenResponse
                {
                    AccessToken = accessTokenResponse.access_token,
                    RefreshToken = accessTokenResponse.refresh_token,
                    ExpiresInSeconds = accessTokenResponse.expires_in,
                    IssuedUtc = flow.Clock.UtcNow
                };

                var credential = new UserCredential(flow, Environment.UserName, token);

                _credential = credential;

                return credential;
            }

            return null;
        }

        public bool IsTokenValid()
        {
            return _credential.Token.IsExpired(_credential.Flow.Clock) == false;
        }

        public List<Message> GetList()
        {
            List<Message> result = new List<Message>();
            UsersResource.MessagesResource.ListRequest request = _service.Users.Messages.List("me");
            request.Q = "from:(noreply@tradingview.com) label:[YOUR_GMAIL_LABEL_FOR_TRADINGVIEW_EMAILS] is:unread";

            do
            {
                try
                {
                    ListMessagesResponse response = request.Execute();
                    result.AddRange(response.Messages);
                    request.PageToken = response.NextPageToken;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            } while (!String.IsNullOrEmpty(request.PageToken));

            return result;
        }

        public AlertMessage GetMessageAlert(string messageId)
        {
            var messageContent = _service.Users.Messages.Get("me", messageId).Execute();
            var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(messageContent.InternalDate.Value).DateTime;
            var body = Base64UrlDecode(messageContent.Payload.Body.Data).Split("####")[1];
            var jsonBody = "{" + body + "}";
            var alertMessage = JsonConvert.DeserializeObject<AlertMessage>(jsonBody);
            alertMessage.CreatedAt = createdAt;
            alertMessage.MessageId = messageId;

            return alertMessage;
        }

        public void MarkMessageAsUnread(string messageId)
        {
            var markAsReadRequest = new ModifyMessageRequest { RemoveLabelIds = new[] { "UNREAD" } };
            var response = _service.Users.Messages.Modify(markAsReadRequest, "me", messageId).Execute();
        }

        private string Base64UrlDecode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "<strong>Message body was not returned from Google</strong>";

            string InputStr = input.Replace("-", "+").Replace("_", "/");
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(InputStr));

        }
    }
}
