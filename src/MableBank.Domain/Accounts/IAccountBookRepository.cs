namespace MableBank.Domain.Accounts;

/// <summary>Stores each company's <see cref="AccountBook"/>, keyed by company id.</summary>
public interface IAccountBookRepository
{
    AccountBook? Find(string companyId);

    /// <summary>Stores the book for a company, replacing any book it already had.</summary>
    void Save(string companyId, AccountBook book);
}
