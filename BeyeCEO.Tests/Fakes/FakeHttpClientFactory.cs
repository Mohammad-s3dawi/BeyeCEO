using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BeyeCEO.Tests.Fakes
{
    // Hand-rolled fake (no mocking library in this project). Every outgoing
    // request is routed through `Responder`, which the test configures per
    // scenario. `Requests` records every call for assertions like "robots
    // disallow means the page itself is never fetched."
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(Responder(request));
        }
    }

    public class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    // Wraps a byte array so tests can observe how many bytes
    // PageMetadataFetcher actually reads before stopping early (byte-budget
    // / found-early-exit behavior) instead of just how many were available.
    public class CountingStream : System.IO.Stream
    {
        private readonly System.IO.MemoryStream _inner;
        public int TotalBytesRead { get; private set; }

        public CountingStream(byte[] data) => _inner = new System.IO.MemoryStream(data);

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            TotalBytesRead += read;
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            var read = _inner.Read(buffer, offset, count);
            TotalBytesRead += read;
            return Task.FromResult(read);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
        public override void Flush() => _inner.Flush();
        public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    // HttpContent backed by a CountingStream so a test can assert how much
    // of a (large) response body was actually read.
    public class CountingStreamContent : HttpContent
    {
        public CountingStream Stream { get; }

        public CountingStreamContent(byte[] data) => Stream = new CountingStream(data);

        protected override Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context) =>
            throw new NotSupportedException("Only used as a response body read via ReadAsStreamAsync.");

        protected override bool TryComputeLength(out long length)
        {
            length = Stream.Length;
            return true;
        }

        protected override Task<System.IO.Stream> CreateContentReadStreamAsync() => Task.FromResult<System.IO.Stream>(Stream);
    }
}
