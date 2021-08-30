﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Uno.Extensions.Navigation
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseNavigation(
        this IHostBuilder builder)
        {
            return builder
                .ConfigureServices(sp =>
                {
                    _ = sp.AddSingleton<INavigationMapping, NavigationMapping>()
                           .AddSingleton<IFrameWrapper, FrameWrapper>()
                           .AddSingleton<INavigationAdapter, FrameNavigationAdapter>()
                           .AddSingleton<INavigationService, NavigationService>();
                });
        }
    }
}
