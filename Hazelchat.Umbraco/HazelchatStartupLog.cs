// This file is part of Hazelchat for Umbraco.
// (C) 2026 Hazelchat. MIT License; see LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Hazelchat.Umbraco;

/// <summary>One line in Umbraco's log at startup saying whether the chat is shown.</summary>
internal sealed class HazelchatStartupLog : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private readonly IOptionsMonitor<HazelchatOptions> _options;
    private readonly ILogger<HazelchatStartupLog> _logger;

    public HazelchatStartupLog(IOptionsMonitor<HazelchatOptions> options, ILogger<HazelchatStartupLog> logger)
    {
        _options = options;
        _logger = logger;
    }

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        HazelchatOptions options = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.InstallationCode))
        {
            _logger.LogInformation("Hazelchat: no installation code is set (Hazelchat:InstallationCode), so no chat is shown.");
        }
        else if (Key.FromInput(options.InstallationCode) is null)
        {
            _logger.LogWarning("Hazelchat: Hazelchat:InstallationCode is not a valid installation code, so no chat is shown. Paste the code from Hazelchat or its 64-character key.");
        }
        else
        {
            _logger.LogInformation("Hazelchat: the chat is added to content pages{Consent}.", options.RequireConsent ? " and waits for the consent manager" : string.Empty);
        }
    }
}
