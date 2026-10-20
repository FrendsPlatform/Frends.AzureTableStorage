using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Frends.AzureTableStorage.QueryEntities.Attributes;
using NUnit.Framework;

namespace Frends.AzureTableStorage.QueryEntities.Tests.AttributesTests;

[TestFixture]
internal class RequiredIfAttributeTests
{
    [Test]
    public void RejectsMissingValuesWhenConditionMatches()
    {
        object[] missingValues =
        [
            null,
            string.Empty,
            " \t ",
            Array.Empty<int>(),
            new List<int>(),
            new HashSet<int>(),
            Enumerable.Empty<int>(),
        ];

        foreach (var value in missingValues)
        {
            var result = Validate(value);
            Assert.That(result?.ErrorMessage, Is.EqualTo("Items is required."), $"Value: {value}");
        }
    }

    [Test]
    public void AcceptsPresentValuesWhenConditionMatches()
    {
        object[] presentValues = ["value", new[] { 1 }, new List<int> { 1 }, Enumerable.Repeat(1, 1), 0];

        foreach (var value in presentValues)
        {
            Assert.That(Validate(value), Is.Null, $"Value: {value}");
        }
    }

    [Test]
    public void AcceptsMissingValuesWhenConditionDoesNotMatch()
    {
        Assert.That(Validate(Array.Empty<int>(), "Disabled"), Is.Null);
        Assert.That(Validate(null, "Disabled"), Is.Null);
    }

    private static ValidationResult Validate(object value, string mode = "Enabled")
    {
        var model = new Example { Mode = mode };
        var context = new ValidationContext(model) { MemberName = nameof(Example.Items) };
        return new RequiredIfAttribute(nameof(Example.Mode), "Enabled").GetValidationResult(value, context);
    }

    private sealed class Example
    {
        public string Mode { get; set; }

        public object Items { get; set; }
    }
}
