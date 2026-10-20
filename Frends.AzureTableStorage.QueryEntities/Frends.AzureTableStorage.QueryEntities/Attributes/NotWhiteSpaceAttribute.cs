using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Frends.AzureTableStorage.QueryEntities.Attributes;

/// <summary>
/// Validates that a value is not made up only of white-space.
/// Null and empty values are valid. For collections, every item must be non-empty and not white-space-only.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal class NotWhiteSpaceAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var invalid = value switch
        {
            string { Length: > 0 } s => string.IsNullOrWhiteSpace(s),
            string => false,
            IEnumerable items => items.Cast<object>().Any(i => i is not string s || string.IsNullOrWhiteSpace(s)),
            _ => false,
        };

        return invalid
            ? new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must not contain white-space-only values.")
            : ValidationResult.Success;
    }
}
