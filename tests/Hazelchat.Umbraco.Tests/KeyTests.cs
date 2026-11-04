// The package's pure functions: the TYPO3/Kirby/Grav/October key vectors,
// the tag and its placement, and which responses may be changed.

using System.Text;
using Hazelchat.Umbraco;
using Xunit;

namespace Hazelchat.Umbraco.Tests;

public class KeyTests
{
    private static readonly string K = string.Concat(Enumerable.Repeat("a1", 32));
    private static readonly string Other = string.Concat(Enumerable.Repeat("b2", 32));
    private static readonly string Code = "<script src=\"https://app.hazelchat.co/widget.js\" data-agent=\"" + K + "\" defer></script>";
    private static readonly string TagOff = "<script src=\"https://app.hazelchat.co/widget.js\" id=\"hazelchat-umbraco\" defer data-agent=\"" + K + "\"></script>";
    private const string PageHtml = "<!doctype html><html><body><main>Werkstatt</main></body></html>";

    [Fact]
    public void A_bare_key_or_a_pasted_code_gives_the_key()
    {
        Assert.Equal(K, Key.FromInput(K));
        Assert.Equal(K, Key.FromInput("  " + K + "\n"));
        Assert.Equal(K, Key.FromInput(Code));
        Assert.Equal(K, Key.FromInput("<script data-agent='" + K + "'></script>"));
        Assert.Equal(K, Key.FromInput("<script data-agent=" + K + " defer>"));
        Assert.Equal(K, Key.FromInput("<!-- Hazelchat -->" + Code));
        Assert.Equal(K, Key.FromInput("<script src=\"https://evil.example/x.js\" data-agent=\"" + K + "\"></script>"));
    }

    [Fact]
    public void Anything_else_gives_no_key()
    {
        Assert.Null(Key.FromInput("<script data-agent=\"" + K + "ff\">"));
        Assert.Null(Key.FromInput(K.ToUpperInvariant()));
        Assert.Null(Key.FromInput("<script data-agent=\"" + K.ToUpperInvariant() + "\">"));
        Assert.Null(Key.FromInput("sk-" + K));
        Assert.Null(Key.FromInput("<script data-agent=\"sk-" + K + "\">"));
        Assert.Null(Key.FromInput(K[1..]));
        Assert.Null(Key.FromInput(K + "a"));
        Assert.Null(Key.FromInput(new string('x', 1000) + Code));
        Assert.Null(Key.FromInput(null));
        Assert.Null(Key.FromInput(""));
        Assert.Null(Key.FromInput("   "));
        Assert.Null(Key.FromInput(K[..32] + "\n" + K[32..]));
    }

    [Fact]
    public void Two_or_look_alike_attributes_and_commented_keys_are_refused()
    {
        Assert.Null(Key.FromInput("data-agent=\"" + K + "\" data-agent=\"" + Other + "\""));
        Assert.Null(Key.FromInput(Code + Code));
        Assert.Null(Key.FromInput("<script DATA-AGENT=\"" + Other + "\" data-agent=\"" + K + "\">"));
        Assert.Null(Key.FromInput("<script xdata-agent=\"" + K + "\">"));
        Assert.Null(Key.FromInput("<script data-agent-x=\"" + K + "\">"));
        Assert.Null(Key.FromInput("<script x-data-agent=\"" + K + "\">"));
        Assert.Null(Key.FromInput("<!-- <script data-agent=\"" + K + "\"></script> -->"));
        Assert.Null(Key.FromInput("<!-- " + Code));
    }

    [Fact]
    public void The_tag_has_the_fixed_address_the_id_defer_and_the_key()
    {
        Assert.Null(Key.Tag(null, false));
        Assert.Null(Key.Tag("", true));
        Assert.Null(Key.Tag("not-a-key", true));
        Assert.Null(Key.Tag(K.ToUpperInvariant(), false));
        Assert.Equal(TagOff, Key.Tag(K, false));
        Assert.Equal(TagOff.Replace("\"></script>", "\" data-require-consent=\"true\"></script>"), Key.Tag(K, true));
        Assert.Equal("https://app.hazelchat.co/widget.js", Key.ScriptUrl);
    }

    [Fact]
    public void The_tag_goes_just_before_the_last_body_end()
    {
        Assert.Equal("<!doctype html><html><body><main>Werkstatt</main>" + TagOff + "\n</body></html>", Key.Insert(PageHtml, TagOff));
        Assert.Equal("<body><script>var s='</body>'</script>" + TagOff + "\n</BODY>", Key.Insert("<body><script>var s='</body>'</script></BODY>", TagOff));
        Assert.Equal("<body>x" + TagOff + "\n</body >", Key.Insert("<body>x</body >", TagOff));
        Assert.Equal("<body><header>W</header><p></bodyx></p>" + TagOff + "\n</body>", Key.Insert("<body><header>W</header><p></bodyx></p></body>", TagOff));
    }

    [Fact]
    public void Fragments_tagged_pages_and_hand_pasted_codes_stay_as_they_are()
    {
        Assert.Equal("<p>Werkstatt</p>", Key.Insert("<p>Werkstatt</p>", TagOff));
        Assert.Equal("<p></bodyx></p>", Key.Insert("<p></bodyx></p>", TagOff));
        Assert.Equal(PageHtml, Key.Insert(PageHtml, null));
        string once = Key.Insert(PageHtml, TagOff);
        Assert.Equal(once, Key.Insert(once, TagOff));
        Assert.Equal("<body><script id='hazelchat-umbraco'></script></body>", Key.Insert("<body><script id='hazelchat-umbraco'></script></body>", TagOff));
        string pasted = "<body><main>W</main>" + Code + "</body>";
        Assert.Same(pasted, Key.Insert(pasted, TagOff));
        Assert.NotEqual("<body><div id=\"hazelchat-umbraco-x\"></div></body>", Key.Insert("<body><div id=\"hazelchat-umbraco-x\"></div></body>", TagOff));
    }
}

public class PageTests
{
    private static readonly string Tag = Key.Tag(string.Concat(Enumerable.Repeat("a1", 32)), false)!;

    [Theory]
    [InlineData(200, "text/html; charset=utf-8", null, true)]
    [InlineData(200, "text/html", "", true)]
    [InlineData(200, "TEXT/HTML; Charset=UTF-8", "identity", true)]
    [InlineData(404, "text/html; charset=utf-8", null, false)]
    [InlineData(302, "text/html; charset=utf-8", null, false)]
    [InlineData(200, "application/json; charset=utf-8", null, false)]
    [InlineData(200, "application/xhtml+xml", null, false)]
    [InlineData(200, "text/xml", null, false)]
    [InlineData(200, "text/html; charset=iso-8859-1", null, false)]
    [InlineData(200, "text/html; charset=utf-8", "gzip", false)]
    [InlineData(200, "text/html; charset=utf-8", "br", false)]
    [InlineData(200, null, null, false)]
    public void Only_uncompressed_utf8_html_with_status_200_is_changed(int status, string? type, string? encoding, bool expected) =>
        Assert.Equal(expected, Page.IsEditableHtml(status, type, encoding));

    [Theory]
    [InlineData("/umbraco", true)]
    [InlineData("/umbraco/backoffice/", true)]
    [InlineData("/UMBRACO/management/api/v1/server", true)]
    [InlineData("/App_Plugins/Foo/foo.js", true)]
    [InlineData("/media/abcd1234/photo.jpg", true)]
    [InlineData("/sb/bundle.css.v123", true)]
    [InlineData("/install", true)]
    [InlineData("/", false)]
    [InlineData("/about-us/", false)]
    [InlineData("/umbraco-partners/", false)]
    [InlineData("/mediation/", false)]
    [InlineData(null, false)]
    public void The_back_office_and_files_are_never_buffered(string? path, bool skipped) =>
        Assert.Equal(skipped, Page.IsSkippedPath(path));

    [Fact]
    public void A_page_is_spliced_in_utf8_and_keeps_its_byte_order_mark()
    {
        byte[] page = Encoding.UTF8.GetBytes("﻿<html><body>Grüße</body></html>");
        byte[]? changed = Page.Splice(page, Tag);
        Assert.NotNull(changed);
        Assert.Equal("﻿<html><body>Grüße" + Tag + "\n</body></html>", Encoding.UTF8.GetString(changed!));
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, changed![..3]);
    }

    [Fact]
    public void Pages_that_stay_the_same_give_null()
    {
        Assert.Null(Page.Splice(Encoding.UTF8.GetBytes("<p>no body end</p>"), Tag));
        Assert.Null(Page.Splice(Encoding.UTF8.GetBytes("<body>" + Tag + "</body>"), Tag));
        Assert.Null(Page.Splice(new byte[] { 0x3C, 0x62, 0xFF, 0xFE, 0x3E }, Tag));
    }
}
