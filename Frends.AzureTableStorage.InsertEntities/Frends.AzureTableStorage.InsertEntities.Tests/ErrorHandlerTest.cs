using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Frends.AzureTableStorage.InsertEntities.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.InsertEntities.Tests;

[TestFixture]
internal class ErrorHandlerTest : TestBase
{
    private const string CustomErrorMessage = "CustomErrorMessage";

    [Test]
    public void Should_Throw_Error_When_ThrowErrorOnFailure_Is_True()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = true;

        Func<Task> action = async () =>
            await AzureTableStorage.InsertEntities(ValidInput(), DefaultConnection(), options, CancellationToken.None);

        var ex = Assert.ThrowsAsync<ArgumentException>(action);
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public async Task Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.InsertEntities(ValidInput(), DefaultConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Is.Not.Empty);
    }

    [Test]
    public void Should_Use_Custom_ErrorMessageOnFailure()
    {
        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;

        Func<Task> action = async () =>
            await AzureTableStorage.InsertEntities(ValidInput(), DefaultConnection(), options, CancellationToken.None);

        var ex = Assert.ThrowsAsync<Exception>(action);

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Contains.Substring(CustomErrorMessage));
    }

    [Test]
    public void Should_Throw_ValidationException_When_Input_Is_Invalid()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = true;

        Func<Task> action = async () =>
            await AzureTableStorage.InsertEntities(DefaultInput(), DefaultConnection(), options, CancellationToken.None);

        var ex = Assert.ThrowsAsync<ValidationException>(action);

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain("TableName"));
    }

    private static Input ValidInput() => new()
    {
        TableName = "testtable",
        Entities = ToEntitiesJson(new { PartitionKey = "pk", RowKey = "1", Name = "Test" }),
    };
}
