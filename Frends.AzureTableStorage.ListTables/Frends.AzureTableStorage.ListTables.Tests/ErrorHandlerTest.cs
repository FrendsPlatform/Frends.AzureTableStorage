using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Frends.AzureTableStorage.ListTables.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.ListTables.Tests;

[TestFixture]
internal class ErrorHandlerTest : TestBase
{
    private const string CustomErrorMessage = "Custom error occurred during table listing";

    [Test]
    public void Should_Throw_Error_When_ThrowErrorOnFailure_Is_True()
    {
        var input = DefaultInput();
        var connection = new Connection
        {
            ConnectionMethod = ConnectionMethod.ConnectionString,
            ConnectionString = "Invalid",
        };
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = true;

        Func<Task> action = async () => { await AzureTableStorage.ListTables(input, connection, options, CancellationToken.None); };
        var ex = Assert.ThrowsAsync<ArgumentException>(action);
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public async Task Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        var input = DefaultInput();
        var connection = new Connection
        {
            ConnectionMethod = ConnectionMethod.ConnectionString,
            ConnectionString = "Invalid",
        };
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.ListTables(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Is.Not.Empty);
    }

    [Test]
    public void Should_Use_Custom_ErrorMessageOnFailure()
    {
        var input = DefaultInput();
        var connection = new Connection
        {
            ConnectionMethod = ConnectionMethod.ConnectionString,
            ConnectionString = "Invalid",
        };
        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;
        options.ThrowErrorOnFailure = true;

        Func<Task> action = async () => { await AzureTableStorage.ListTables(input, connection, options, CancellationToken.None); };
        var ex = Assert.ThrowsAsync<Exception>(action);
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain(CustomErrorMessage));
    }

    [Test]
    public async Task Should_Return_Custom_ErrorMessage_When_ThrowErrorOnFailure_Is_False()
    {
        var input = DefaultInput();
        var connection = new Connection
        {
            ConnectionMethod = ConnectionMethod.ConnectionString,
            ConnectionString = "Invalid",
        };
        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.ListTables(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain(CustomErrorMessage));
    }

    [Test]
    public async Task Should_Always_Throw_OperationCanceledException()
    {
        Assume.That(ConnectionString, Is.Not.Empty, "Connection string is required for cancellation test.");

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => AzureTableStorage.ListTables(DefaultInput(), DefaultConnectionStringConnection(), DefaultOptions(), cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void Should_Throw_ValidationException_When_Input_Is_Null()
    {
        var options = DefaultOptions();

        Func<Task> action = async () =>
        {
            await AzureTableStorage.ListTables(null, DefaultConnectionStringConnection(), options, CancellationToken.None);
        };

        var ex = Assert.ThrowsAsync<ValidationException>(action);

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain("Validated object can't be null"));
    }
}
