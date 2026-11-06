using System.Text.RegularExpressions;
using Banking.Infrastructure.Services;
using Banking.Tests.Fakes;

namespace Banking.Tests.Infrastructure;

public class AccountNumberGeneratorTests
{
    private static readonly DateTimeOffset Today = new(2026, 6, 18, 10, 30, 0, TimeSpan.Zero);

    private static AccountNumberGenerator CreateGenerator(int seed = 42) =>
        new(new FakeTimeProvider(Today), new Random(seed));

    [Fact]
    public void Generate_HasExactLengthOf17Characters()
    {
        var number = CreateGenerator().Generate();

        Assert.Equal(17, number.Length); // "ACC-" (4) + "YYYYMMDD" (8) + "-" (1) + "XXXX" (4)
    }

    [Fact]
    public void Generate_StartsWithAccPrefix()
    {
        var number = CreateGenerator().Generate();

        Assert.StartsWith("ACC-", number);
    }

    [Fact]
    public void Generate_ContainsTodaysDate()
    {
        var number = CreateGenerator().Generate();

        Assert.Equal("20260618", number.Substring(4, 8));
    }

    [Fact]
    public void Generate_MatchesExactFormat()
    {
        var number = CreateGenerator().Generate();

        Assert.Matches(new Regex(@"^ACC-\d{8}-\d{4}$"), number);
    }

    [Fact]
    public void Generate_AlwaysProducesFourDigitSuffix_EvenWithLeadingZeros()
    {
        var generator = CreateGenerator();

        for (var i = 0; i < 2_000; i++)
        {
            var suffix = generator.Generate().Split('-')[2];

            Assert.Equal(4, suffix.Length);
            Assert.All(suffix, c => Assert.True(char.IsDigit(c)));
        }
    }

    [Fact]
    public void Generate_ProducesDistinctNumbersInABasicSample()
    {
        // Con semilla fija la prueba es determinista (no depende del azar).
        var generator = CreateGenerator(seed: 123);

        var numbers = Enumerable.Range(0, 20).Select(_ => generator.Generate()).ToList();

        Assert.Equal(numbers.Count, numbers.Distinct().Count());
    }

    [Fact]
    public void Generate_UsesTheCurrentDateOfTheClock()
    {
        var clock = new FakeTimeProvider(Today);
        var generator = new AccountNumberGenerator(clock, new Random(1));

        clock.Advance(TimeSpan.FromDays(1));

        Assert.Equal("20260619", generator.Generate().Substring(4, 8));
    }
}
