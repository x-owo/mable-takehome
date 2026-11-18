using MableBank.Domain.Accounts;

namespace MableBank.Tests.Domain.Accounts;

public class MoneyTests
{
    public class Constructor
    {
        [TestCase(0)]
        [TestCase(500)]
        [TestCase(320.5)]
        [TestCase(25.60)]
        [TestCase(-10.25)]
        public void accepts_amounts_with_up_to_two_decimal_places(decimal amount) =>
            new Money(amount).Amount.ShouldBe(amount);

        [TestCase(10.005)]
        [TestCase(0.001)]
        public void rejects_fractions_of_a_cent(decimal amount) =>
            Should.Throw<ArgumentException>(() => new Money(amount));
    }

    public class Arithmetic
    {
        [Test]
        public void adds_amounts() =>
            (new Money(1200m) + new Money(25.60m)).ShouldBe(new Money(1225.60m));

        [Test]
        public void subtracts_amounts_and_can_go_negative() =>
            (new Money(100m) - new Money(150.50m)).ShouldBe(new Money(-50.50m));
    }

    public class Comparison
    {
        [Test]
        public void orders_by_amount()
        {
            (new Money(1m) < new Money(2m)).ShouldBeTrue();
            (new Money(2m) >= new Money(2m)).ShouldBeTrue();
        }

        [Test]
        public void is_positive_only_when_greater_than_zero()
        {
            new Money(0.01m).IsPositive.ShouldBeTrue();
            Money.Zero.IsPositive.ShouldBeFalse();
            new Money(-1m).IsPositive.ShouldBeFalse();
        }
    }

    public class Formatting
    {
        [TestCase(5000, "5000.00")]
        [TestCase(320.5, "320.50")]
        [TestCase(-3, "-3.00")]
        public void always_shows_two_decimal_places(decimal amount, string expected) =>
            new Money(amount).ToString().ShouldBe(expected);
    }
}
