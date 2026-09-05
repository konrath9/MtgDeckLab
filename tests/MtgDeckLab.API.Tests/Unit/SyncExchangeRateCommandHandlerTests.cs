using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MtgDeckLab.Application.ExchangeRates.Commands.SyncExchangeRate;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.API.Tests.Unit;

public class SyncExchangeRateCommandHandlerTests
{
    private sealed class FakeFetcher(decimal? rate) : IExchangeRateFetcher
    {
        public Task<decimal?> FetchUsdToBrlAsync(CancellationToken ct = default) => Task.FromResult(rate);
    }

    private sealed class FakeStore : IExchangeRateStore
    {
        public CachedExchangeRate? Current { get; private set; }
        public void Set(decimal usdToBrl, DateTimeOffset fetchedAt) => Current = new CachedExchangeRate(usdToBrl, fetchedAt);
    }

    [Fact]
    public async Task Handle_WhenFetchSucceeds_UpdatesTheStore()
    {
        var store = new FakeStore();
        var handler = new SyncExchangeRateCommandHandler(
            new FakeFetcher(5.25m), store, NullLogger<SyncExchangeRateCommandHandler>.Instance);

        var result = await handler.Handle(new SyncExchangeRateCommand(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.UsdToBrl.Should().Be(5.25m);
        store.Current.Should().NotBeNull();
        store.Current!.UsdToBrl.Should().Be(5.25m);
    }

    [Fact]
    public async Task Handle_WhenFetchFails_LeavesThePreviousCachedValueUntouched()
    {
        var store = new FakeStore();
        store.Set(5.00m, DateTimeOffset.UtcNow.AddDays(-1));

        var handler = new SyncExchangeRateCommandHandler(
            new FakeFetcher(null), store, NullLogger<SyncExchangeRateCommandHandler>.Instance);

        var result = await handler.Handle(new SyncExchangeRateCommand(), CancellationToken.None);

        result.Success.Should().BeFalse();
        // Um valor de ontem ainda é melhor exibição do que nenhum — a falha não deve apagar o cache.
        store.Current.Should().NotBeNull();
        store.Current!.UsdToBrl.Should().Be(5.00m);
    }
}
