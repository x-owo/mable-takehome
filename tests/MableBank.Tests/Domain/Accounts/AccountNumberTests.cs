using MableBank.Domain.Accounts;

namespace MableBank.Tests.Domain.Accounts;

public class AccountNumberTests
{
    public class Constructor
    {
        [Test]
        public void accepts_exactly_16_digits() =>
            new AccountNumber("1111234522226789").Value.ShouldBe("1111234522226789");

        [TestCase("111123452222678")]
        [TestCase("11112345222267890")]
        [TestCase("")]
        public void rejects_the_wrong_number_of_digits(string value) =>
            Should.Throw<ArgumentException>(() => new AccountNumber(value));

        [TestCase("1111-34522226789")]
        [TestCase("111123452222678a")]
        [TestCase(" 111123452222678")]
        public void rejects_non_digit_characters(string value) =>
            Should.Throw<ArgumentException>(() => new AccountNumber(value));
    }

    public class Equality
    {
        [Test]
        public void two_numbers_with_the_same_digits_are_equal() =>
            new AccountNumber("1111234522226789").ShouldBe(new AccountNumber("1111234522226789"));
    }
}
