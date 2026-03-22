using LottyAB.Application.Exceptions;
using LottyAB.Application.Services;
using LottyAB.Application.Targeting;

namespace LottyAB.Tests;

public class TargetingDslTests
{
    private static readonly ValueComparer m_Comparer = new();
    private static readonly TargetingParser m_Parser = new(m_Comparer);
    private static readonly TargetingEvaluatorService m_Evaluator = new(m_Parser);

    private static bool Eval(string rule, Dictionary<string, object> attrs)
        => m_Parser.Parse(rule).Evaluate(attrs);

    [Theory]
    [InlineData("country == \"US\"", "US", true)]
    [InlineData("country == \"US\"", "UK", false)]
    [InlineData("country == \"us\"", "US", true)]
    [InlineData("country != \"US\"", "US", false)]
    [InlineData("country != \"US\"", "UK", true)]
    public void StringComparison_ReturnsExpected(string rule, string value, bool expected)
        => Assert.Equal(expected, Eval(rule, new() { ["country"] = value }));

    [Theory]
    [InlineData("age > 18", 25.0, true)]
    [InlineData("age > 18", 18.0, false)]
    [InlineData("age > 18", 10.0, false)]
    [InlineData("age >= 18", 18.0, true)]
    [InlineData("age >= 18", 20.0, true)]
    [InlineData("age >= 18", 17.0, false)]
    [InlineData("age < 30", 25.0, true)]
    [InlineData("age < 30", 30.0, false)]
    [InlineData("age < 30", 35.0, false)]
    [InlineData("age <= 30", 30.0, true)]
    [InlineData("age <= 30", 25.0, true)]
    [InlineData("age <= 30", 31.0, false)]
    public void NumericComparison_ReturnsExpected(string rule, double value, bool expected)
        => Assert.Equal(expected, Eval(rule, new() { ["age"] = value }));

    [Fact]
    public void Equals_Numeric_ReturnsTrue()
        => Assert.True(Eval("age == 25", new() { ["age"] = 25.0 }));

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Equals_Bool_ReturnsExpected(bool attrValue, bool expected)
        => Assert.Equal(expected, Eval("premium == true", new() { ["premium"] = attrValue }));

    [Theory]
    [InlineData("country IN [\"US\", \"CA\", \"UK\"]", "US", true)]
    [InlineData("country IN [\"US\", \"CA\"]", "DE", false)]
    [InlineData("country NOT IN [\"US\", \"CA\"]", "UK", true)]
    [InlineData("country NOT IN [\"US\", \"CA\"]", "US", false)]
    public void SetOperator_ReturnsExpected(string rule, string value, bool expected)
        => Assert.Equal(expected, Eval(rule, new() { ["country"] = value }));

    [Theory]
    [InlineData("US", 25.0, true)]
    [InlineData("US", 10.0, false)]
    [InlineData("UK", 25.0, false)]
    [InlineData("UK", 10.0, false)]
    public void And_Operator_ReturnsExpected(string country, double age, bool expected)
        => Assert.Equal(expected, Eval("country == \"US\" AND age > 18",
            new() { ["country"] = country, ["age"] = age }));

    [Theory]
    [InlineData("US", true)]
    [InlineData("UK", true)]
    [InlineData("DE", false)]
    public void Or_Operator_ReturnsExpected(string country, bool expected)
        => Assert.Equal(expected, Eval("country == \"US\" OR country == \"UK\"",
            new() { ["country"] = country }));

    [Theory]
    [InlineData("US", false)]
    [InlineData("UK", true)]
    public void Not_Operator_ReturnsExpected(string country, bool expected)
        => Assert.Equal(expected, Eval("NOT country == \"US\"", new() { ["country"] = country }));

    [Theory]
    [InlineData("US", 18.0, true)]
    [InlineData("CA", 25.0, true)]
    [InlineData("US", 17.0, false)]
    [InlineData("UK", 25.0, false)]
    public void Nested_AndOr_ReturnsExpected(string country, double age, bool expected)
        => Assert.Equal(expected, Eval("(country == \"US\" OR country == \"CA\") AND age >= 18",
            new() { ["country"] = country, ["age"] = age }));

    [Theory]
    [InlineData("US", 21.0, true, true)]
    [InlineData("US", 21.0, false, false)]
    [InlineData("US", 16.0, true, false)]
    [InlineData("UK", 21.0, true, false)]
    public void Nested_ThreeLevelAnd_ReturnsExpected(string country, double age, bool premium, bool expected)
        => Assert.Equal(expected, Eval("country == \"US\" AND age >= 18 AND premium == true",
            new() { ["country"] = country, ["age"] = age, ["premium"] = premium }));

    [Fact]
    public void MissingAttribute_ReturnsFalse()
        => Assert.False(Eval("country == \"US\"", new() { ["city"] = "LA" }));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_Throws(string rule)
        => Assert.Throws<UnprocessableEntityException>(() => m_Parser.Parse(rule));

    [Fact]
    public void Parse_InvalidComparison_Throws()
        => Assert.Throws<UnprocessableEntityException>(() => m_Parser.Parse("just_a_field"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Evaluator_EmptyOrNullRule_ReturnsTrue(string? rule)
        => Assert.True(m_Evaluator.EvaluateRule(rule, new() { ["country"] = "US" }));

    [Fact]
    public void Evaluator_NullAttributes_ReturnsFalse()
        => Assert.False(m_Evaluator.EvaluateRule("country == \"US\"", null));

    [Fact]
    public void Evaluator_EmptyAttributes_ReturnsFalse()
        => Assert.False(m_Evaluator.EvaluateRule("country == \"US\"", new()));

    [Fact]
    public void Evaluator_InvalidSyntax_ReturnsFalse()
        => Assert.False(m_Evaluator.EvaluateRule("!!!invalid!!!", new() { ["country"] = "US" }));

    [Theory]
    [InlineData("country == \"US\"", "US", true)]
    [InlineData("country == \"US\"", "UK", false)]
    public void Evaluator_SimpleRule_ReturnsExpected(string rule, string country, bool expected)
        => Assert.Equal(expected, m_Evaluator.EvaluateRule(rule, new() { ["country"] = country }));

    [Theory]
    [InlineData("US", 25.0, true)]
    [InlineData("DE", 25.0, false)]
    [InlineData("US", 10.0, false)]
    public void Evaluator_ComplexRule_ReturnsExpected(string country, double age, bool expected)
        => Assert.Equal(expected, m_Evaluator.EvaluateRule(
            "country IN [\"US\", \"CA\"] AND age > 18",
            new() { ["country"] = country, ["age"] = age }));
}