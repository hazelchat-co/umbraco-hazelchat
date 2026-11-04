// This file is part of Hazelchat for Umbraco.
// (C) 2026 Hazelchat. MIT License; see LICENSE.

namespace Hazelchat.Umbraco;

/// <summary>
/// The "Hazelchat" section of the site's configuration (appsettings.json,
/// or environment variables such as Hazelchat__InstallationCode).
/// </summary>
public sealed class HazelchatOptions
{
    public const string Section = "Hazelchat";

    /// <summary>The installation code from Hazelchat, or just its 64-character key.</summary>
    public string? InstallationCode { get; set; }

    /// <summary>Let the chat start only once the site's consent manager allows it.</summary>
    public bool RequireConsent { get; set; }
}
