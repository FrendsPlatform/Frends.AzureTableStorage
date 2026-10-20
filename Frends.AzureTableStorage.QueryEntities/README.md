# Frends.AzureTableStorage.QueryEntities

Query a single page of entities from Azure Table Storage using an optional OData filter and property selection.

[![Build status](https://github.com/FrendsPlatform/Frends.AzureTableStorage/actions/workflows/QueryEntities_main_release.yml/badge.svg)](https://github.com/FrendsPlatform/Frends.AzureTableStorage/actions/workflows/QueryEntities_main_release.yml)
![Code Coverage](https://app-github-custom-badges.azurewebsites.net/Badge?key=FrendsPlatform/Frends.AzureTableStorage/Frends.AzureTableStorage.QueryEntities|main)
[![Repository License](https://img.shields.io/github/license/FrendsPlatform/Frends.AzureTableStorage?label=License)](https://github.com/FrendsPlatform/Frends.AzureTableStorage/blob/main/LICENSE)

## Installing

You can install the Task via Frends UI Task View.

## Querying entities

The task takes `Input`, `Connection` and `Options` parameters:

- `Input.TableName` (required): name of an existing table.
- `Input.Filter`: an OData filter such as `PartitionKey eq 'pk1' and RowKey ge '10'`. Empty means no filter.
- `Input.Select`: an array of property names, such as `["Name", "Price"]`. Empty means all properties.
- `Input.Top`: maximum entities **per page**, from 1 to 1000 (default 1000). Azure Table Storage can return fewer, or even zero, entities while providing a continuation token.
- `Input.ContinuationToken`: leave empty for the first page; pass the previous result's token to fetch the next page.
- `Connection.ConnectionMethod`: `ConnectionString` (default), `OAuth2`, `SasToken`, `ArcManagedIdentity` or `ArcManagedIdentityCrossTenant`, with the corresponding credentials. The same connection parameters and required-field checks as the other Azure Table Storage tasks apply.
- `Options.ThrowErrorOnFailure`: throw on failure (default `true`) or return a failed result with `Error.Message` and `Error.AdditionalInfo`. `Options.ErrorMessageOnFailure` customizes the failure message. Cancellation is always propagated.

For example, set `TableName` to `orders`, `Filter` to `PartitionKey eq 'pk1'`, `Select` to `["Name", "Price"]` and `Top` to `100`. A successful result contains `Success`, `Entities` (JSON objects with entity keys and available properties, including `odata.etag`), `Count` and `ContinuationToken`. Pass `ContinuationToken` back with the **same table, filter, selection and page size** until it is null or empty. Do not stop on an empty `Entities` list if a continuation token is present. A missing table or invalid OData filter is a failure rather than an empty result.

## Building

### Clone a copy of the repository

`git clone https://github.com/FrendsPlatform/Frends.AzureTableStorage.git`

### Build the project

`dotnet build`

### Run tests

`dotnet test`

The paging and validation tests run without an Azure account. To run the storage integration tests locally, copy `Frends.AzureTableStorage.QueryEntities.Tests/.env.example` to `.env` and supply a Table Storage connection string. OAuth2 and SAS tests require their respective credentials.

### Create a NuGet package

`dotnet pack --configuration Release`

### StyleCop.Analyzers Version
This project uses StyleCop.Analyzers 1.2.0-beta.556, as recommended by the author, to get the latest fixes and improvements not available in the last stable release.
