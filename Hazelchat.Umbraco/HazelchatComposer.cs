// This file is part of Hazelchat for Umbraco.
// (C) 2026 Hazelchat. MIT License; see LICENSE.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Hazelchat.Umbraco;

/// <summary>
/// Umbraco finds this composer in every installed package at startup, so
/// adding the package is the whole installation: no Program.cs change.
/// </summary>
public sealed class HazelchatComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<HazelchatOptions>(builder.Config.GetSection(HazelchatOptions.Section));
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
            options.AddFilter(new UmbracoPipelineFilter("Hazelchat")
            {
                PostPipeline = app => app.UseMiddleware<HazelchatMiddleware>(),
            }));
        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, HazelchatStartupLog>();
    }
}
