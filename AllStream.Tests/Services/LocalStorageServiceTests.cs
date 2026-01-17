using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AllStream.Shared.Services;
using FluentAssertions;
using Microsoft.JSInterop;
using Xunit;

namespace AllStream.Tests.Services
{
    public class LocalStorageServiceTests
    {
        private class FakeJSRuntime : IJSRuntime
        {
            public string? LastIdentifier { get; private set; }
            public object? LastArg1 { get; private set; }
            public object? LastArg2 { get; private set; }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                LastIdentifier = identifier;
                LastArg1 = args != null && args.Length > 0 ? args[0] : null;
                LastArg2 = args != null && args.Length > 1 ? args[1] : null;
                return ValueTask.FromResult<TValue>(default!);
            }

            public ValueTask<TValue> InvokeAsync<TValue>(
                string identifier,
                CancellationToken cancellationToken,
                object?[]? args
            )
            {
                return InvokeAsync<TValue>(identifier, args);
            }
        }

        [Fact]
        public async Task SetItemAsync_SerializesAndInvokesLocalStorage()
        {
            var js = new FakeJSRuntime();
            var svc = new LocalStorageService(js);

            var obj = new { A = 1, B = "x" };
            await svc.SetItemAsync("key", obj);

            js.LastIdentifier.Should().Be("localStorage.setItem");
            js.LastArg1.Should().Be("key");
            js.LastArg2.Should().Be(JsonSerializer.Serialize(obj));
        }
    }
}
