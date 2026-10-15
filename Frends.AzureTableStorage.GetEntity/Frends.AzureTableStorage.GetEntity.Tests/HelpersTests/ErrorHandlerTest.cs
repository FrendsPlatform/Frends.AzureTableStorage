using NUnit.Framework;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Frends.AzureTableStorage.GetEntity.Tests.HelpersTests;

[TestFixture]
internal class ErrorHandlerTest : TestBase
{
    private const string CustomErrorMessage = "CustomErrorMessage";

    [Test]
    public async Task Should_Throw_Error_When_ThrowErrorOnFailure_Is_True()
    {
        Func<Task> action = async () =>
            await AzureTableStorage.GetEntity(DefaultInput(), DefaultConnection(), DefaultOptions(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ValidationException>(action);
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public async Task Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;
        var result = await AzureTableStorage.GetEntity(DefaultInput(), DefaultConnection(), options, CancellationToken.None);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Should_Use_Custom_ErrorMessageOnFailure()
    {
        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;
        var ex = Assert.Throws<Exception>(() =>
             AzureTableStorage.GetEntity(DefaultInput(), DefaultConnection(), options, CancellationToken.None).GetAwaiter().GetResult());
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Contains.Substring(CustomErrorMessage));
    }
}
