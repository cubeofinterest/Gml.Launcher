using System.Net.Http;
using Splat;

namespace Gml.Launcher.Core.Services
{
    public static class HttpClientFactory
    {
        public static HttpClient CreateClient()
        {
#if DEBUG
            var devLoggingService = Locator.Current.GetService<IDevLoggingService>();
            
            if (devLoggingService != null)
            {
                var handler = devLoggingService.CreateNetworkLoggingHandler(new HttpClientHandler());
                return new HttpClient(handler);
            }
#endif
            return new HttpClient();
        }
        
        public static HttpClient CreateClientWithHandler(HttpMessageHandler handler)
        {
#if DEBUG
            var devLoggingService = Locator.Current.GetService<IDevLoggingService>();
            
            if (devLoggingService != null)
            {
                var loggingHandler = devLoggingService.CreateNetworkLoggingHandler(handler);
                return new HttpClient(loggingHandler);
            }
#endif
            return new HttpClient(handler);
        }
    }
}