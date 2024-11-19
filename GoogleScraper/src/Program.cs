using Microsoft.Extensions.DependencyInjection;
using GoogleScraper.Services;
using MyHostBuilder = VideoToolsCore.Services.HostBuilder;
using VideoToolsCore.DataAccess;
using GoogleScraper.Auth;
using System.Net.Http.Headers;
using System.Net;
using CasCap.Services;
using Polly;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CasCap.Models;

namespace GoogleScraper;

class Program
{  
  public static async Task Main()
  {
    var host = MyHostBuilder.CreateHost<Settings>(services =>
    {
      services.AddTransient<AuthorizationHandler>();
      
      // This creates http clients for CasCap's GooglePhotosService and my GoogleApiCurrentUserService to use.
      // Before making any request, it will set the Authorization header by calling GetAuthHeader on GoogleLoginService.
      // GoogleLoginService does not use this http client; it uses one created by the google oauth library.
      // Most of the configuration comes from the CasCap library.
      services.AddHttpClient("SharedClient", c => 
      {
        c.BaseAddress = new Uri(CasCap.Models.RequestUris.BaseAddress);
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        c.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        c.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        c.Timeout = Timeout.InfiniteTimeSpan;
      })
      .AddHttpMessageHandler<AuthorizationHandler>()
      .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
      {
          AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
      })
      .AddTransientHttpErrorPolicy(policyBuilder =>
      {
          return policyBuilder.RetryAsync(retryCount: 3);
      })
      //https://github.com/aspnet/AspNetCore/issues/6804
      .SetHandlerLifetime(Timeout.InfiniteTimeSpan);


      services.AddSingleton<Scraper>()
        .AddSingleton<VideoContextFactory>()
        // can't use CasCap's DI extension method, IServiceCollection.AddGooglePhotos, because it will
        // give the GooglePhotosService a new http client but we need it use the one defined above
        // which gets its auth header set by the AuthorizationHandler
        .AddSingleton<GooglePhotosService>(sp =>
        {
          var logger = sp.GetRequiredService<ILogger<GooglePhotosService>>();
          var options = sp.GetRequiredService<IOptions<GooglePhotosOptions>>();
          var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
          var httpClient = clientFactory.CreateClient("SharedClient");
          return new GooglePhotosService(logger, options, httpClient);
        })
        .AddSingleton<GoogleLoginService>()
        .AddSingleton<GoogleApiCurrentUserService>();
    });
 
    var googleScraper = host.Services.GetRequiredService<Scraper>();
    await googleScraper.ScrapeVideos();
    await googleScraper.ScrapeAlbums();
  }
}

  

  
