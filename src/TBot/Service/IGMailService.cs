using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TBot.Models;

namespace TBot.Service
{
    public interface IGMailService
    {
        List<Message> GetList();
        AlertMessage GetMessageAlert(string messageId);
        void MarkMessageAsUnread(string messageId);
        Task<UserCredential> CreateCredentialsAsync();
        bool IsTokenValid();
    }
}
