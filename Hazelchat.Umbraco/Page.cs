// This file is part of Hazelchat for Umbraco.
// (C) 2026 Hazelchat. MIT License; see LICENSE.

using System.Text;
using Microsoft.Net.Http.Headers;

namespace Hazelchat.Umbraco;

/// <summary>
/// Which responses the package may read and change, as plain functions so
/// they can be tested without a web server. Everything not named here
/// passes through unread.
/// </summary>
public static class Page
{
    /// <summary>Pages larger than this are sent on unchanged.</summary>
    public const int MaxBytes = 4 * 1024 * 1024;

    // Never content pages: the back office and its APIs, package and media
    // files, Smidge bundles and the installer. Checked before anything is
    // buffered; the Umbraco route check decides the rest.
    private static readonly string[] SkippedPrefixes = ["/umbraco", "/App_Plugins", "/media", "/sb", "/install"];

    public static bool IsSkippedPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }
        foreach (string prefix in SkippedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && (path.Length == prefix.Length || path[prefix.Length] == '/'))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// A finished 200 HTML page in UTF-8 that nothing has compressed. Read
    /// from the response headers at the first byte of the body.
    /// </summary>
    public static bool IsEditableHtml(int status, string? contentType, string? contentEncoding)
    {
        if (status != 200 || string.IsNullOrEmpty(contentType))
        {
            return false;
        }
        if (!string.IsNullOrEmpty(contentEncoding) && !string.Equals(contentEncoding.Trim(), "identity", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (!MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? type)
            || !type.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return !type.Charset.HasValue
            || type.Charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase)
            || type.Charset.Equals("utf8", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The page with the tag, or null when it stays byte-for-byte as it was
    /// (no &lt;/body&gt;, already tagged, pasted by hand, not valid UTF-8).
    /// </summary>
    public static byte[]? Splice(ReadOnlySpan<byte> page, string tag)
    {
        string html;
        try
        {
            html = new UTF8Encoding(false, true).GetString(page);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
        string changed = Key.Insert(html, tag);
        if (ReferenceEquals(changed, html))
        {
            return null;
        }
        // A byte-order mark the page started with is a character of the
        // string, so it is written back as it was.
        return Encoding.UTF8.GetBytes(changed);
    }
}
