// This file is part of Hazelchat for Umbraco.
// (C) 2026 Hazelchat. MIT License; see LICENSE.

using System.Text;
using System.Text.RegularExpressions;

namespace Hazelchat.Umbraco;

/// <summary>
/// The public widget key and the one script tag, as plain functions with no
/// Umbraco dependency, so they can be tested alone. The key is public: it
/// names an agent and grants no access to the Hazelchat account. The rules
/// are the ones of the Kirby, Grav and October plugins.
/// </summary>
public static class Key
{
    /// <summary>The only address this package ever prints.</summary>
    public const string ScriptUrl = "https://app.hazelchat.co/widget.js";

    /// <summary>Hazelchat counts installations of this package by this id.</summary>
    public const string ScriptId = "hazelchat-umbraco";

    /// <summary>Room for a pasted installation code; the key itself is 64 characters.</summary>
    public const int MaxInput = 1000;

    private static readonly TimeSpan Limit = TimeSpan.FromMilliseconds(250);
    private static readonly Regex Hex64 = new(@"\A[a-f0-9]{64}\z", RegexOptions.CultureInvariant, Limit);
    private static readonly Regex Comment = new("<!--.*?-->", RegexOptions.Singleline | RegexOptions.CultureInvariant, Limit);
    // Exactly one data-agent attribute; look-alikes such as xdata-agent or
    // data-agent-x are not it, and two are ambiguous.
    private const string Attribute = @"(?<![A-Za-z0-9_:-])data-agent(?![A-Za-z0-9_:-])\s*=";
    private static readonly Regex AnyAttribute = new(Attribute, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Limit);
    private static readonly Regex AttributeValue = new(
        Attribute + @"\s*(?:""([a-f0-9]{64})""|'([a-f0-9]{64})'|([a-f0-9]{64})(?=[\s>/]|\z))",
        RegexOptions.CultureInvariant,
        Limit);
    private static readonly Regex OwnId = new(@"\bid\s*=\s*[""']?" + ScriptId + @"[""'\s>]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Limit);
    private static readonly Regex BodyEnd = new(@"</body\s*>", RegexOptions.IgnoreCase | RegexOptions.RightToLeft | RegexOptions.CultureInvariant, Limit);

    public static bool Valid(string? key) => key is not null && Hex64.IsMatch(key);

    /// <summary>
    /// The setting takes the whole installation code or just the key. A bare
    /// 64-hex value is the key; otherwise the text must hold exactly one
    /// data-agent attribute (outside HTML comments) whose value is 64
    /// lowercase hex. Anything else gives null, and nothing is printed.
    /// </summary>
    public static string? FromInput(string? value)
    {
        if (value is null)
        {
            return null;
        }
        value = value.Trim();
        if (value.Length == 0 || Encoding.UTF8.GetByteCount(value) > MaxInput)
        {
            return null;
        }
        if (Valid(value))
        {
            return value;
        }

        try
        {
            // A key that is only mentioned in a comment is not the code.
            value = Comment.Replace(value, string.Empty);
            if (value.Contains("<!--", StringComparison.Ordinal))
            {
                return null;
            }
            if (AnyAttribute.Matches(value).Count != 1)
            {
                return null;
            }
            Match match = AttributeValue.Match(value);
            if (!match.Success)
            {
                return null;
            }
            string key = match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value;
            return Valid(key) ? key : null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    /// <summary>
    /// The script tag, or null when nothing may be printed. No inline code,
    /// no data-platform and no address but <see cref="ScriptUrl"/>.
    /// </summary>
    public static string? Tag(string? key, bool requireConsent)
    {
        if (!Valid(key))
        {
            return null;
        }
        return "<script src=\"" + ScriptUrl + "\" id=\"" + ScriptId + "\" defer data-agent=\"" + key + "\""
            + (requireConsent ? " data-require-consent=\"true\"" : string.Empty)
            + "></script>";
    }

    /// <summary>
    /// Puts the tag just before the page's last &lt;/body&gt;. A page without
    /// &lt;/body&gt; (a fragment), one that already carries the tag, or one
    /// where the installation code was pasted by hand stays as it is. Only a
    /// real &lt;/body&gt; counts: &lt;/bodyx&gt; does not.
    /// </summary>
    public static string Insert(string html, string? tag)
    {
        if (tag is null || html.Contains(ScriptUrl, StringComparison.OrdinalIgnoreCase))
        {
            return html;
        }
        try
        {
            if (OwnId.IsMatch(html))
            {
                return html;
            }
            Match end = BodyEnd.Match(html);
            if (!end.Success)
            {
                return html;
            }
            return html[..end.Index] + tag + "\n" + html[end.Index..];
        }
        catch (RegexMatchTimeoutException)
        {
            return html;
        }
    }
}
