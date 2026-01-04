using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using MoviesApp.Shared.Models;

namespace MoviesApp.Shared.Services;

public static class ServiceBuilder
{
    public static IServiceCollection AddSharedServices(
        this IServiceCollection services,
        Func<IServiceProvider, IFormFactor> formFactorFactory,
        Settings settings
    )
    {
        services.AddSingleton<IFormFactor>(formFactorFactory);
        services.AddHttpClient<IMovieService, ImdbApiDevMovieService>(c =>
        {
            c.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            if (!string.IsNullOrWhiteSpace(settings.TmdbApiKey))
            {
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    settings.TmdbApiKey
                );
            }
            c.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
        });
        services.AddHttpClient<CDNLiveService>(c =>
        {
            c.BaseAddress = new Uri("https://api.cdn-live.tv/api/v1/");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            c.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
            );
        });
        return services;
    }
}
