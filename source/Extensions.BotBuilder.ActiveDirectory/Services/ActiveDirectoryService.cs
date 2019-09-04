﻿using Extensions.BotBuilder.ActiveDirectory.Domain;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Extensions.BotBuilder.ActiveDirectory.Services
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly ActiveDirectoryConfig config = null;

        public ActiveDirectoryService(string environmentName, string contentRootPath)
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(contentRootPath)
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
              .AddEnvironmentVariables();

            var configuration = builder.Build();

            config = new ActiveDirectoryConfig();
            configuration.GetSection("ActiveDirectoryConfig").Bind(config);

            if (string.IsNullOrEmpty(config.ValidAudience))
                throw new ArgumentException("Missing value in ActiveDirectoryConfig -> ValidAudience");

            if (string.IsNullOrEmpty(config.ValidIssuer))
                throw new ArgumentException("Missing value in ActiveDirectoryConfig -> ValidIssuer");
        }

        public ActiveDirectoryConfig GetConfiguration() => config;

        public async Task<bool> ValidateTokenAsync(ITurnContext turnContext, string validAudience, string validIssuer, bool validateLifetime, string issuerSigningKey = "")
        {
            bool result = true;
            string token = string.Empty;
            if (ValidateContent(turnContext))
            {
                try
                {
                    var channelObj = turnContext.Activity.ChannelData.ToString();
                    var channeldata = Newtonsoft.Json.Linq.JObject.Parse(channelObj);
                    token = channeldata["token"].ToString();
                    await TokenValidationAsync(token, validAudience, validIssuer, validateLifetime, issuerSigningKey);
                }
                catch (SecurityTokenException ex)
                {
                    Console.WriteLine(ex.Message);
                    result = false;
                }
            }

            return result;
        }

        private bool ValidateContent(ITurnContext turnContext)
        {
            bool result = true;
            string token = string.Empty;

            if (turnContext.Activity.ChannelData == null)
            {
                result = false;
            }
            else
            {
                try
                {
                    var channelObj = turnContext.Activity.ChannelData.ToString();
                    var channeldata = Newtonsoft.Json.Linq.JObject.Parse(channelObj);
                    token = channeldata["token"].ToString();

                    if (channeldata == null)
                    {
                        result = false;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(token))
                        {
                            result = false;
                        }
                    }
                }
                catch
                {
                    result = false;
                }
            }

            return result;
        }

        private async Task<JwtSecurityToken> TokenValidationAsync(string token, string validAudience, string validIssuer, bool validateLifetime, string issuerSigningKey = "")
        {
            TokenValidationParameters validationParameters = new TokenValidationParameters();
            validationParameters.ValidateAudience = true;
            validationParameters.ValidAudience = validAudience;
            validationParameters.ValidateIssuer = true;
            validationParameters.ValidIssuer = validIssuer;
            validationParameters.ValidateIssuerSigningKey = true;
            validationParameters.ValidateLifetime = validateLifetime;

            if (string.IsNullOrEmpty(issuerSigningKey))
            {
                string stsDiscoveryEndpoint = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

                ConfigurationManager<OpenIdConnectConfiguration> configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
                OpenIdConnectConfiguration config = await configManager.GetConfigurationAsync();
                validationParameters.IssuerSigningKeys = config.SigningKeys;
            }
            else
            {
                var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.Default.GetBytes(issuerSigningKey));
                validationParameters.IssuerSigningKey = securityKey;
            }

            JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();
            SecurityToken jwt;
            IdentityModelEventSource.ShowPII = false;
            ClaimsPrincipal claimsPrincipal = tokendHandler.ValidateToken(token, validationParameters, out jwt);

            return jwt as JwtSecurityToken;
        }
    }
}