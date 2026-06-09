using WalletApp.Hexagonal.Domain.Model;
using WalletApp.Hexagonal.Domain.Ports.Output;

namespace WalletApp.Hexagonal.Tests.InMemory;

/// <summary>Simple in-memory test double for IWalletRepository; no mocking framework required.</summary>
internal class InMemoryWalletRepository : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> _store = new();

    public Task<Wallet?> GetByIdAsync(Guid id)
        => Task.FromResult(_store.TryGetValue(id, out var w) ? w : null);

    public Task<Wallet?> GetByOwnerIdAsync(string ownerId)
        => Task.FromResult(_store.Values.FirstOrDefault(w => w.OwnerId == ownerId));

    public Task AddAsync(Wallet wallet)
    {
        _store[wallet.Id] = wallet;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Wallet wallet)
    {
        _store[wallet.Id] = wallet;
        return Task.CompletedTask;
    }
}
