// This file is part of Hazelchat for Umbraco.
// (C) 2026 Hazelchat. MIT License; see LICENSE.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Routing;

namespace Hazelchat.Umbraco;

/// <summary>
/// Adds the tag to pages Umbraco renders from its content. Runs after
/// Umbraco's own middleware (an Umbraco pipeline filter), so it sees the
/// finished page before any response compression placed in front of
/// Umbraco. With no valid key, or for anything but a GET outside the back
/// office and files, it only passes the request on.
/// </summary>
internal sealed class HazelchatMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<HazelchatOptions> _options;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public HazelchatMiddleware(RequestDelegate next, IOptionsMonitor<HazelchatOptions> options, IUmbracoContextAccessor umbracoContextAccessor)
    {
        _next = next;
        _options = options;
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        HazelchatOptions options = _options.CurrentValue;
        string? tag = Key.Tag(Key.FromInput(options.InstallationCode), options.RequireConsent);
        if (tag is null || !HttpMethods.IsGet(context.Request.Method) || Page.IsSkippedPath(context.Request.Path.Value))
        {
            await _next(context);
            return;
        }

        Stream original = context.Response.Body;
        var page = new PageStream(original, () => IsContentPage(context));
        context.Response.Body = page;
        try
        {
            await _next(context);
            byte[]? buffered = page.Finish();
            if (buffered is null)
            {
                return;
            }
            byte[] body = buffered;
            byte[]? changed = Page.Splice(buffered, tag);
            if (changed is not null && !(context.Response.HasStarted && context.Response.ContentLength is not null))
            {
                body = changed;
                if (!context.Response.HasStarted && context.Response.ContentLength is not null)
                {
                    context.Response.ContentLength = body.Length;
                }
            }
            await original.WriteAsync(body, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = original;
        }
    }

    // Asked once, at the first byte of the body, when status and headers
    // are final: a 200 UTF-8 HTML page, not compressed, that Umbraco
    // routed to published content, outside preview.
    private bool IsContentPage(HttpContext context)
    {
        HttpResponse response = context.Response;
        if (!Page.IsEditableHtml(response.StatusCode, response.ContentType, response.Headers[HeaderNames.ContentEncoding]))
        {
            return false;
        }
        if (context.Features.Get<UmbracoRouteValues>() is null)
        {
            return false;
        }
        return !(_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext) && umbracoContext.InPreviewMode);
    }
}

/// <summary>
/// Holds a content page until it is complete; sends anything else, and
/// pages over <see cref="Page.MaxBytes"/>, straight through.
/// </summary>
internal sealed class PageStream : Stream
{
    private readonly Stream _inner;
    private readonly Func<bool> _isContentPage;
    private MemoryStream? _buffer;
    private bool _decided;

    public PageStream(Stream inner, Func<bool> isContentPage)
    {
        _inner = inner;
        _isContentPage = isContentPage;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    /// <summary>The held page, or null when everything was sent already.</summary>
    public byte[]? Finish() => _buffer?.ToArray();

    // True while the page is held, false once it is passing through.
    private bool Holding(int count)
    {
        if (!_decided)
        {
            _decided = true;
            if (_isContentPage())
            {
                _buffer = new MemoryStream();
            }
        }
        return _buffer is not null && _buffer.Length + count <= Page.MaxBytes;
    }

    private async ValueTask ReleaseAsync(CancellationToken cancellationToken)
    {
        if (_buffer is null)
        {
            return;
        }
        MemoryStream held = _buffer;
        _buffer = null;
        await _inner.WriteAsync(held.GetBuffer().AsMemory(0, (int)held.Length), cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        if (Holding(source.Length))
        {
            _buffer!.Write(source.Span);
            return;
        }
        await ReleaseAsync(cancellationToken);
        await _inner.WriteAsync(source, cancellationToken);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (Holding(count))
        {
            _buffer!.Write(buffer, offset, count);
            return;
        }
        if (_buffer is not null)
        {
            MemoryStream held = _buffer;
            _buffer = null;
            _inner.Write(held.GetBuffer(), 0, (int)held.Length);
        }
        _inner.Write(buffer, offset, count);
    }

    // A held page is sent whole at the end; flushing it early would send
    // the headers before the length is known.
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _buffer is null && _decided ? _inner.FlushAsync(cancellationToken) : Task.CompletedTask;

    public override void Flush()
    {
        if (_buffer is null && _decided)
        {
            _inner.Flush();
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
