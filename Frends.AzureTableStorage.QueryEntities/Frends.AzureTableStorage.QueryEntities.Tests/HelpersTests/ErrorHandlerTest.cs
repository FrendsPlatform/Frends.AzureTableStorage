using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Frends.AzureTableStorage.QueryEntities.Definitions;
using Frends.AzureTableStorage.QueryEntities.Helpers;
using NUnit.Framework;

namespace Frends.AzureTableStorage.QueryEntities.Tests.HelpersTests;

[TestFixture]
internal class ErrorHandlerTest : TestBase
{
    private const string CustomErrorMessage = "CustomErrorMessage";

    [Test]
    public async Task ShouldThrowValidationErrorWhenThrowErrorOnFailureIsTrue()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            AzureTableStorage.QueryEntities(DefaultInput(), DefaultConnection(), DefaultOptions(), CancellationToken.None));

        Assert.That(ex.Message, Does.Contain("TableName"));
    }

    [Test]
    public async Task ShouldReturnFailedResultWhenThrowErrorOnFailureIsFalse()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.QueryEntities(DefaultInput(), DefaultConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Entities, Is.Null);
        Assert.That(result.Count, Is.Zero);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Error.AdditionalInfo, Is.TypeOf<ValidationException>());
    }

    [Test]
    public void ShouldUseCustomErrorMessageOnFailure()
    {
        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;
        var ex = Assert.Throws<Exception>((Action)(() =>
            AzureTableStorage.QueryEntities(DefaultInput(), DefaultConnection(), options, CancellationToken.None)
                .GetAwaiter().GetResult()));

        Assert.That(ex.Message, Is.EqualTo(CustomErrorMessage));
        Assert.That(ex.InnerException, Is.TypeOf<ValidationException>());
    }

    [Test]
    public async Task ShouldIncludeCustomErrorMessageWhenReturningFailure()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;
        options.ErrorMessageOnFailure = CustomErrorMessage;

        var result = await AzureTableStorage.QueryEntities(DefaultInput(), DefaultConnection(), options, CancellationToken.None);

        Assert.That(result.Error.Message, Does.StartWith($"{CustomErrorMessage}: "));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(1001)]
    public async Task ShouldRejectTopOutsidePageLimit(int top)
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.QueryEntities(
            new Input { TableName = "orders", Top = top }, DefaultConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.AdditionalInfo, Is.TypeOf<ValidationException>());
        Assert.That(result.Error.Message, Does.Contain("Top must be between 1 and 1000."));
    }

    [TestCase(1)]
    [TestCase(1000)]
    public void ShouldAcceptTopAtPageLimit(int top)
    {
        Assert.DoesNotThrow(() => ValidationHandler.Run(
            new Input { TableName = "orders", Top = top },
            new Connection { ConnectionString = "UseDevelopmentStorage=true" },
            DefaultOptions()));
    }

    [Test]
    public async Task ShouldRejectInvalidOptionalQueryParameters()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var inputs = new[]
        {
            (new Input { TableName = "orders", Filter = " \t " }, "Filter"),
            (new Input { TableName = "orders", Select = ["Name", " "] }, "Select"),
            (new Input { TableName = "orders", Select = [null] }, "Select"),
            (new Input { TableName = "orders", ContinuationToken = " \t " }, "ContinuationToken"),
        };

        foreach (var (input, parameter) in inputs)
        {
            var result = await AzureTableStorage.QueryEntities(input, DefaultConnection(), options, CancellationToken.None);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.AdditionalInfo, Is.TypeOf<ValidationException>());
            Assert.That(result.Error.Message, Does.Contain(parameter));
        }
    }

    [TestCase(ConnectionMethod.ConnectionString, "ConnectionString")]
    [TestCase(ConnectionMethod.OAuth2, "ApplicationId")]
    [TestCase(ConnectionMethod.SasToken, "SasToken")]
    [TestCase(ConnectionMethod.ArcManagedIdentity, "StorageAccountName")]
    [TestCase(ConnectionMethod.ArcManagedIdentityCrossTenant, "Scopes")]
    public async Task ShouldRequireCredentialsForSelectedConnectionMethod(ConnectionMethod method, string requiredField)
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.QueryEntities(
            new Input { TableName = "orders" },
            new Connection { ConnectionMethod = method },
            options,
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.AdditionalInfo, Is.TypeOf<ValidationException>());
        Assert.That(result.Error.Message, Does.Contain(requiredField));
    }

    [Test]
    public async Task ShouldRejectInvalidStorageAccountName()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.QueryEntities(
            new Input { TableName = "orders" },
            new Connection
            {
                ConnectionMethod = ConnectionMethod.SasToken,
                SasToken = "?sig=example",
                StorageAccountName = "Invalid_Name",
            },
            options,
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("StorageAccountName must be 3-24 lowercase letters or digits."));
    }

    [Test]
    public async Task ShouldPropagateCancellationEvenWhenFailureIsReturned()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;
        options.ErrorMessageOnFailure = CustomErrorMessage;

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            AzureTableStorage.QueryEntities(
                new Input { TableName = "orders" },
                new Connection { ConnectionString = "UseDevelopmentStorage=true" },
                options,
                source.Token));
    }
}
