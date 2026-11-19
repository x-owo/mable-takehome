using MableBank.Api.Storage;
using MableBank.Domain.Accounts;

namespace MableBank.Tests.Api.Storage;

public class InMemoryAccountBookRepositoryTests
{
    private static AccountBook EmptyBook() => new([]);

    [Test]
    public void finds_nothing_for_a_company_that_has_not_loaded_balances() =>
        new InMemoryAccountBookRepository().Find("alpha-sales").ShouldBeNull();

    [Test]
    public void returns_the_book_saved_for_a_company()
    {
        var repository = new InMemoryAccountBookRepository();
        var book = EmptyBook();

        repository.Save("alpha-sales", book);

        repository.Find("alpha-sales").ShouldBeSameAs(book);
    }

    [Test]
    public void replaces_a_company_book_when_saved_again()
    {
        var repository = new InMemoryAccountBookRepository();
        var replacement = EmptyBook();
        repository.Save("alpha-sales", EmptyBook());

        repository.Save("alpha-sales", replacement);

        repository.Find("alpha-sales").ShouldBeSameAs(replacement);
    }

    [Test]
    public void keeps_each_company_separate()
    {
        var repository = new InMemoryAccountBookRepository();
        repository.Save("alpha-sales", EmptyBook());

        repository.Find("beta-sales").ShouldBeNull();
    }
}
