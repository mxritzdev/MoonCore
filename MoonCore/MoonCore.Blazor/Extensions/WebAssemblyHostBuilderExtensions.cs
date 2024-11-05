﻿using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MoonCore.Blazor.Services;
using MoonCore.Helpers;
using MoonCore.Http.Requests.TokenAuthentication;
using MoonCore.Http.Responses.TokenAuthentication;

namespace MoonCore.Blazor.Extensions;

public static class WebAssemblyHostBuilderExtensions
{
    public static void AddTokenAuthentication(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddScoped(serviceProvider =>
        {
            var httpClient = serviceProvider.GetRequiredService<HttpClient>();
            var localStorageService = serviceProvider.GetRequiredService<LocalStorageService>();
            
            var httpApiClient = new HttpApiClient(httpClient);

            httpApiClient.OnConfigureRequest += async request =>
            {
                // Don't handle auth when calling an authentication endpoint
                if(request.RequestUri?.LocalPath.StartsWith("/_auth") ?? false)
                    return;
                
                // Check expire date
                var expiresAt = await localStorageService.GetDefaulted("ExpiresAt", DateTime.MinValue);

                string accessToken;
                
                if (DateTime.UtcNow > expiresAt) // Expire date reached, refresh access token
                {
                    var refreshToken = await localStorageService.GetStringDefaulted("RefreshToken", "unset");

                    // Call to refresh provider
                    var refreshData = await httpApiClient.PostJson<RefreshResponse>("/_auth/refresh", new RefreshRequest()
                    {
                        RefreshToken = refreshToken
                    });

                    // Save new tokens
                    await localStorageService.SetString("AccessToken", refreshData.AccessToken);
                    await localStorageService.SetString("RefreshToken", refreshData.RefreshToken);
                    await localStorageService.Set("ExpiresAt", refreshData.ExpiresAt);

                    accessToken = refreshData.AccessToken;
                }
                else
                    accessToken = await localStorageService.GetStringDefaulted("AccessToken", "unset");
                
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
            };

            return httpApiClient;
        });
    }
}