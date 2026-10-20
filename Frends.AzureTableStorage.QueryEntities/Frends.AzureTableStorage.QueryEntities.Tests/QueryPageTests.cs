using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Frends.AzureTableStorage.QueryEntities.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.QueryEntities.Tests;

[TestFixture]
internal class QueryPageTests
{
    [Test]
    public async Task ShouldPassQueryParametersAndReturnOnlyTheFirstPage()
    {
        var first = new TableEntity("pk1", "1")
        {
            ["Name"] = "Anna",
            ["Price"] = 10,
            ["Bytes"] = new byte[] { 1, 2, 3 },
        };
        var second = new TableEntity("pk1", "2") { ["Name"] = "Bob" };
        var pages = new RecordingPageable(
            Page<TableEntity>.FromValues([first], "next-page", null),
            Page<TableEntity>.FromValues([second], null, null));
        var client = new StubTableClient(pages);
        var input = new Input
        {
            TableName = "orders",
            Filter = "PartitionKey eq 'pk1'",
            Select = ["Name", "Price"],
            Top = 2,
            ContinuationToken = "previous-page",
        };
        using var source = new CancellationTokenSource();

        var result = await AzureTableStorage.QueryPageAsync(client, input, source.Token);

        Assert.That(client.Filter, Is.EqualTo(input.Filter));
        Assert.That(client.MaxPerPage, Is.EqualTo(input.Top));
        Assert.That(client.Select, Is.EqualTo(new[] { "Name", "Price", "PartitionKey", "RowKey" }));
        Assert.That(client.CancellationToken, Is.EqualTo(source.Token));
        Assert.That(pages.RequestedToken, Is.EqualTo(input.ContinuationToken));
        Assert.That(pages.PagesRead, Is.EqualTo(1));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.Entities, Has.Count.EqualTo(1));
        Assert.That((string)result.Entities[0]["PartitionKey"], Is.EqualTo("pk1"));
        Assert.That((string)result.Entities[0]["RowKey"], Is.EqualTo("1"));
        Assert.That((string)result.Entities[0]["Name"], Is.EqualTo("Anna"));
        Assert.That((int)result.Entities[0]["Price"], Is.EqualTo(10));
        Assert.That((string)result.Entities[0]["Bytes"], Is.EqualTo(Convert.ToBase64String([1, 2, 3])));
        Assert.That(result.ContinuationToken, Is.EqualTo("next-page"));
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task ShouldPreserveContinuationTokenOnEmptyPage()
    {
        var pages = new RecordingPageable(
            Page<TableEntity>.FromValues([], "next-page", null),
            Page<TableEntity>.FromValues([new TableEntity("pk1", "2")], null, null));

        var result = await AzureTableStorage.QueryPageAsync(
            new StubTableClient(pages), new Input(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entities, Is.Empty);
        Assert.That(result.Count, Is.Zero);
        Assert.That(result.ContinuationToken, Is.EqualTo("next-page"));
        Assert.That(pages.PagesRead, Is.EqualTo(1));
    }

    [Test]
    public async Task ShouldOmitOptionalParametersAndReturnFinalPage()
    {
        var pages = new RecordingPageable(
            Page<TableEntity>.FromValues([new TableEntity("pk1", "3")], null, null));
        var client = new StubTableClient(pages);

        var result = await AzureTableStorage.QueryPageAsync(
            client, new Input { TableName = "orders" }, CancellationToken.None);

        Assert.That(client.Filter, Is.Null);
        Assert.That(client.Select, Is.Null);
        Assert.That(client.MaxPerPage, Is.EqualTo(1000));
        Assert.That(pages.RequestedToken, Is.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.ContinuationToken, Is.Null);
    }

    [Test]
    public async Task ShouldReturnEmptyResultWhenNoPageIsReturned()
    {
        var pages = new RecordingPageable();

        var result = await AzureTableStorage.QueryPageAsync(
            new StubTableClient(pages), new Input { TableName = "orders" }, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entities, Is.Empty);
        Assert.That(result.Count, Is.Zero);
        Assert.That(result.ContinuationToken, Is.Null);
    }

    [Test]
    public void ShouldHonorCancellationWhileMappingPage()
    {
        var pages = new RecordingPageable(
            Page<TableEntity>.FromValues([new TableEntity("pk1", "1")], null, null));
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            AzureTableStorage.QueryPageAsync(
                new StubTableClient(pages), new Input { TableName = "orders" }, source.Token));
    }

    private sealed class StubTableClient(RecordingPageable pages) : TableClient
    {
        public string Filter { get; private set; }

        public int? MaxPerPage { get; private set; }

        public string[] Select { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public override AsyncPageable<T> QueryAsync<T>(
            string filter = null,
            int? maxPerPage = null,
            IEnumerable<string> select = null,
            CancellationToken cancellationToken = default)
        {
            if (typeof(T) != typeof(TableEntity))
                throw new NotSupportedException();

            Filter = filter;
            MaxPerPage = maxPerPage;
            Select = select?.ToArray();
            CancellationToken = cancellationToken;
            return (AsyncPageable<T>)(object)pages;
        }
    }

    private sealed class RecordingPageable(params Page<TableEntity>[] pages) : AsyncPageable<TableEntity>
    {
        public string RequestedToken { get; private set; }

        public int PagesRead { get; private set; }

        public override async IAsyncEnumerable<Page<TableEntity>> AsPages(
            string continuationToken = null, int? pageSizeHint = null)
        {
            RequestedToken = continuationToken;
            foreach (var page in pages)
            {
                PagesRead++;
                yield return page;
                await Task.Yield();
            }
        }
    }
}
