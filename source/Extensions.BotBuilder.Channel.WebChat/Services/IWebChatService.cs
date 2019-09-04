﻿using Extensions.BotBuilder.Channel.WebChat.Domain;
using System.Threading.Tasks;

namespace Extensions.BotBuilder.Channel.WebChat.Services
{
    public interface IWebChatService
    {
        WebChatConfig GetConfiguration();

        Task<GenerateResponse> GetDirectLineTokenAsync(string secret);
    }
}