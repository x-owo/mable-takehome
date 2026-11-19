using System.Collections.Concurrent;
using MableBank.Domain.Accounts;

namespace MableBank.Api.Storage;

/// <summary>Keeps every company's accounts in memory for the lifetime of the process.</summary>
public sealed class InMemoryAccountBookRepository : IAccountBookRepository
{
    private readonly ConcurrentDictionary<string, AccountBook> _books = new(StringComparer.Ordinal);

    public AccountBook? Find(string companyId) => _books.GetValueOrDefault(companyId);

    public void Save(string companyId, AccountBook book) => _books[companyId] = book;
}
