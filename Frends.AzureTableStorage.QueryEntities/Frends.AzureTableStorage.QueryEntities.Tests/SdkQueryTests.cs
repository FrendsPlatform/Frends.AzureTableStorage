using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Pipeline;
using Azure.Data.Tables;
using Frends.AzureTableStorage.QueryEntities.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.QueryEntities.Tests;

[TestFixture]
internal class SdkQueryTests
{
    [Test]
    public async Task ShouldSendODataOptionsAndResumeWithTheSdkContinuationToken()
    {
        using var handler = new ScriptedTableHandler();
        var client = CreateClient(handler);
        var input = new Input
        {
            TableName = "orders",
            Filter = "PartitionKey eq 'pk1'",
            Select = ["Name", "Price"],
            Top = 1,
        };

        var first = await AzureTableStorage.QueryPageAsync(client, input, CancellationToken.None);
        input.ContinuationToken = first.ContinuationToken;
        var second = await AzureTableStorage.QueryPageAsync(client, input, CancellationToken.None);

        Assert.That(first.Success, Is.True);
        Assert.That(first.Count, Is.EqualTo(1));
        Assert.That((string)first.Entities[0]["PartitionKey"], Is.EqualTo("pk1"));
        Assert.That((string)first.Entities[0]["RowKey"], Is.EqualTo("1"));
        Assert.That((string)first.Entities[0]["Name"], Is.EqualTo("Anna"));
        Assert.That((string)first.Entities[0]["odata.etag"], Is.EqualTo("W/\"etag1\""));
        Assert.That((int)first.Entities[0]["Price"], Is.EqualTo(10));
        Assert.That(first.Entities[0]["Hidden"], Is.Null);
        Assert.That(first.ContinuationToken, Is.Not.Empty);
        Assert.That(second.Success, Is.True);
        Assert.That(second.Count, Is.EqualTo(1));
        Assert.That((string)second.Entities[0]["Name"], Is.EqualTo("Bob"));
        Assert.That(second.ContinuationToken, Is.Null);
        Assert.That(handler.RequestUris, Has.Count.EqualTo(2));

        foreach (var uri in handler.RequestUris)
        {
            var query = WebUtility.UrlDecode(uri.Query);
            Assert.That(query, Does.Contain("$filter=PartitionKey eq 'pk1'"));
            Assert.That(query, Does.Contain("$select=Name,Price,PartitionKey,RowKey"));
            Assert.That(query, Does.Contain("$top=1"));
        }

        var continuedQuery = WebUtility.UrlDecode(handler.RequestUris[1].Query);
        Assert.That(continuedQuery, Does.Contain("NextPartitionKey=pk1"));
        Assert.That(continuedQuery, Does.Contain("NextRowKey=2"));
    }

    [Test]
    public async Task ShouldExposeAnEmptyServicePageWithAContinuationToken()
    {
        using var handler = new ScriptedTableHandler(emptyFirstPage: true);
        var client = CreateClient(handler);
        var input = new Input { TableName = "orders", Top = 1 };

        var first = await AzureTableStorage.QueryPageAsync(client, input, CancellationToken.None);

        Assert.That(first.Success, Is.True);
        Assert.That(first.Count, Is.Zero);
        Assert.That(first.Entities, Is.Empty);
        Assert.That(first.ContinuationToken, Is.Not.Empty);
        Assert.That(handler.RequestUris, Has.Count.EqualTo(1));

        input.ContinuationToken = first.ContinuationToken;
        var second = await AzureTableStorage.QueryPageAsync(client, input, CancellationToken.None);

        Assert.That(second.Success, Is.True);
        Assert.That(second.Count, Is.EqualTo(1));
        Assert.That((string)second.Entities[0]["Name"], Is.EqualTo("Bob"));
        Assert.That(second.ContinuationToken, Is.Null);
        Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
    }

    private static TableClient CreateClient(HttpMessageHandler handler)
    {
        var options = new TableClientOptions { Transport = new HttpClientTransport(handler) };
        options.Retry.MaxRetries = 0;
        return new TableClient(
            "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;" +
            "AccountKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=;" +
            "TableEndpoint=http://127.0.0.1:10002/devstoreaccount1",
            "orders",
            options);
    }

    private sealed class ScriptedTableHandler(bool emptyFirstPage = false) : HttpMessageHandler
    {
        public List<Uri> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri);
            if (RequestUris.Count > 2)
                throw new InvalidOperationException("The query requested more than two pages.");

            var isFirstPage = RequestUris.Count == 1;
            string content;
            if (isFirstPage && emptyFirstPage)
                content = """{"value":[]}""";
            else if (isFirstPage)
                content = """{"value":[{"PartitionKey":"pk1","RowKey":"1","Name":"Anna","Price":10,"Timestamp":"2024-05-01T10:15:30Z","odata.etag":"W/\"etag1\""}]}""";
            else
                content = """{"value":[{"PartitionKey":"pk1","RowKey":"2","Name":"Bob","Price":20,"Timestamp":"2024-05-01T10:15:30Z","odata.etag":"W/\"etag2\""}]}""";

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };
            response.Headers.TryAddWithoutValidation("x-ms-request-id", Guid.NewGuid().ToString());
            response.Headers.TryAddWithoutValidation("x-ms-version", "2019-02-02");
            if (isFirstPage)
            {
                response.Headers.TryAddWithoutValidation("x-ms-continuation-NextPartitionKey", "pk1");
                response.Headers.TryAddWithoutValidation("x-ms-continuation-NextRowKey", "2");
            }

            return Task.FromResult(response);
        }
    }
}
