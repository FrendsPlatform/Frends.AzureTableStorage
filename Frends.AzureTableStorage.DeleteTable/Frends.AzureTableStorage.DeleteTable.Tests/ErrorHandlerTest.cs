using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Frends.AzureTableStorage.DeleteTable.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.DeleteTable.Tests;

[TestFixture]
internal class ErrorHandlerTest : TestBase
{
    private const string CustomErrorMessage = "Custom error occurred during table creation";

    [Test]
    public void Should_Throw_Error_When_ThrowErrorOnFailure_Is_True()
    {
        var input = new Input { TableName = "TestTable" };
        var connection = new Connection
        {
            ConnectionMethod = ConnectionMethod.ConnectionString,
            ConnectionString = "Invalid",
        };
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = true;

        Func<Task> action = async () => { await AzureTableStorage.DeleteTable(input, connection, options, CancellationToken.None); };
        var ex = Assert.ThrowsAsync<ArgumentException>(action);
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public async Task Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        var input = new Input { TableName = "TestTable" };
        var connection = new Connection
        {
            ConnectionMethod = ConnectionMethod.ConnectionString,
            ConnectionString = "Invalid",
        };
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteTable(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Is.Not.Empty);
    }

    [Test]
    public void Should_Use_Custom_ErrorMessageOnFailure()
    {
        var input = new Input { TableName = "TestTable" };
        var connection = new Connection
        {
            ConnectionMethod = ConnectionMethod.ConnectionString,
            ConnectionString = "Invalid",
        };
        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;
        options.ThrowErrorOnFailure = true;

        Func<Task> action = async () => { await AzureTableStorage.DeleteTable(input, connection, options, CancellationToken.None); };
        var ex = Assert.ThrowsAsync<Exception>(action);
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain(CustomErrorMessage));
    }

    [Test]
    public async Task Should_Return_Custom_ErrorMessage_When_ThrowErrorOnFailure_Is_False()
    {
        var input = new Input { TableName = "TestTable" };
        var connection = new Connection
        {
            ConnectionMethod = ConnectionMethod.ConnectionString,
            ConnectionString = "Invalid",
        };
        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteTable(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain(CustomErrorMessage));
    }

    [Test]
    public void Should_Always_Throw_OperationCanceledException()
    {
        var input = new Input { TableName = "TestTable" };
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false; // Even with this set to false

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> action = async () => { await AzureTableStorage.DeleteTable(input, DefaultConnectionStringConnection(), options, cts.Token); };
        var ex = Assert.ThrowsAsync<TaskCanceledException>(action);
    }

    [Test]
    public void Should_Throw_ValidationException_When_TableName_Is_Empty()
    {
        var input = new Input { TableName = string.Empty };
        var connection = new Connection
        {
            ConnectionMethod = ConnectionMethod.ConnectionString,
            ConnectionString = "valid-connection-string",
        };
        var options = DefaultOptions();

        Func<Task> action = async () => { await AzureTableStorage.DeleteTable(input, connection, options, CancellationToken.None); };
        var ex = Assert.ThrowsAsync<ValidationException>(action);

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain("TableName"));
    }
}
