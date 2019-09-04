﻿using Extensions.BotBuilder.ActiveDirectory.Domain;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;

namespace Extensions.BotBuilder.ActiveDirectory.Services
{
    public interface IActiveDirectoryService
    {
        ActiveDirectoryConfig GetConfiguration();

        Task<bool> ValidateTokenAsync(ITurnContext turnContext, string validAudience, string validIssuer, bool validateLifetime, string issuerSigningKey = "");
    }
}