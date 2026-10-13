using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Frends.AzureTableStorage.UpdateEntities.Attributes;

/// <summary>
/// Validates that a property is required if another property has a specific value.
/// If a property is null, an empty collection, or an empty or white-space-only string, validation fails.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal class RequiredIfAttribute(string dependentProperty, params object[] targetValues) : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var field = validationContext.ObjectType.GetProperty(dependentProperty);

        if (field == null)
            return new ValidationResult($"Unknown property: {dependentProperty}");

        var dependentValue = field.GetValue(validationContext.ObjectInstance);

        if (!targetValues.Contains(dependentValue)) return ValidationResult.Success;

        if (value == null ||
            (value is string s && string.IsNullOrWhiteSpace(s)) ||
            (value is IEnumerable collection && !collection.Cast<object>().Any()))
        {
            return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required.");
        }

        return ValidationResult.Success;
    }
}
