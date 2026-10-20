using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Frends.AzureTableStorage.QueryEntities.Attributes;
using NUnit.Framework;

namespace Frends.AzureTableStorage.QueryEntities.Tests.AttributesTests;

[TestFixture]
internal class NotWhiteSpaceAttributeTests
{
    [Test]
    public void RejectsWhiteSpaceOnlyValues()
    {
        object[] invalidValues =
        [
            " ",
            " \t ",
            new[] { "a", " " },
            new[] { "a", string.Empty },
            new[] { "a", null },
            new List<string> { "\t" },
        ];

        foreach (var value in invalidValues)
        {
            var result = Validate(value);
            Assert.That(
                result?.ErrorMessage,
                Is.EqualTo("Items must not contain white-space-only values."),
                $"Value: {value}");
        }
    }

    [Test]
    public void AcceptsValidValues()
    {
        object[] validValues =
        [
            null,
            string.Empty,
            "value",
            " value ",
            Array.Empty<string>(),
            new[] { "a", "b" },
            new List<string> { "a" },
            0,
        ];

        foreach (var value in validValues)
        {
            Assert.That(Validate(value), Is.Null, $"Value: {value}");
        }
    }

    [Test]
    public void UsesCustomErrorMessage()
    {
        var context = new ValidationContext(new Example()) { MemberName = nameof(Example.Items) };
        var result = new NotWhiteSpaceAttribute { ErrorMessage = "Custom." }.GetValidationResult(" ", context);

        Assert.That(result?.ErrorMessage, Is.EqualTo("Custom."));
    }

    private static ValidationResult Validate(object value)
    {
        var context = new ValidationContext(new Example()) { MemberName = nameof(Example.Items) };
        return new NotWhiteSpaceAttribute().GetValidationResult(value, context);
    }

    private sealed class Example
    {
        public object Items { get; set; }
    }
}
