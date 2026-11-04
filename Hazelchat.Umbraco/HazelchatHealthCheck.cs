// This file is part of Hazelchat for Umbraco.
// (C) 2026 Hazelchat. MIT License; see LICENSE.

using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.HealthChecks;

namespace Hazelchat.Umbraco;

/// <summary>
/// A "Hazelchat" group in Settings → Health Check: whether the installation
/// code is set and valid, and whether the chat waits for consent. It
/// changes nothing.
/// </summary>
[HealthCheck("4b0d8f3e-2a6c-4e1b-9c57-7d2f1a8e6b30", "Hazelchat AI agent", Description = "Checks the Hazelchat installation code in the site's settings.", Group = "Hazelchat")]
public sealed class HazelchatHealthCheck : HealthCheck
{
    private const string Guide = "https://hazelchat.co/integrations/umbraco/";
    private const string Signup = "https://app.hazelchat.co/signup?utm_source=umbraco&utm_medium=package&utm_campaign=health-check";
    private readonly IOptionsMonitor<HazelchatOptions> _options;

    public HazelchatHealthCheck(IOptionsMonitor<HazelchatOptions> options) => _options = options;

    // Umbraco 13–16 (the .NET 8 build) ask GetStatus; 17 and 18 ask
    // GetStatusAsync and no longer have GetStatus.
#if NET10_0_OR_GREATER
    public override Task<IEnumerable<HealthCheckStatus>> GetStatusAsync()
#else
    public override Task<IEnumerable<HealthCheckStatus>> GetStatus()
#endif
    {
        HazelchatOptions options = _options.CurrentValue;
        var statuses = new List<HealthCheckStatus>();
        if (string.IsNullOrWhiteSpace(options.InstallationCode))
        {
            statuses.Add(new HealthCheckStatus("No installation code is set, so no chat is shown. Add Hazelchat:InstallationCode to the site's settings. No Hazelchat account yet? Create a free one.")
            {
                ResultType = StatusResultType.Info,
                ReadMoreLink = Signup,
            });
        }
        else if (Key.FromInput(options.InstallationCode) is null)
        {
            statuses.Add(new HealthCheckStatus("Hazelchat:InstallationCode is not a valid installation code, so no chat is shown. Paste the code from Hazelchat or its 64-character key.")
            {
                ResultType = StatusResultType.Error,
                ReadMoreLink = Guide,
            });
        }
        else
        {
            statuses.Add(new HealthCheckStatus("The installation code is set and valid. The chat is added to every content page.")
            {
                ResultType = StatusResultType.Success,
                ReadMoreLink = Guide,
            });
        }
        statuses.Add(new HealthCheckStatus(options.RequireConsent
            ? "Wait for consent: on. The chat starts once the site's consent manager allows it."
            : "Wait for consent: off. The chat keeps only session storage; turn on Hazelchat:RequireConsent to let a consent manager decide.")
        {
            ResultType = StatusResultType.Info,
            ReadMoreLink = Guide + "#consent",
        });
        return Task.FromResult<IEnumerable<HealthCheckStatus>>(statuses);
    }

#if !NET10_0_OR_GREATER
    public override HealthCheckStatus ExecuteAction(HealthCheckAction action) =>
        throw new InvalidOperationException("The Hazelchat check has no actions.");
#endif
}
