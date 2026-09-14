# Frends.AzureTableStorage.DeleteTable

Deletes a table from Azure Table Storage.

[![DeleteTable_build](https://github.com/FrendsPlatform/Frends.AzureTableStorage/actions/workflows/DeleteTable_test_on_main.yml/badge.svg)](https://github.com/FrendsPlatform/Frends.AzureTableStorage/actions/workflows/DeleteTable_test_on_main.yml)
![Coverage](https://app-github-custom-badges.azurewebsites.net/Badge?key=FrendsPlatform/Frends.AzureTableStorage/Frends.AzureTableStorage.DeleteTable|main)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

## Installing

You can install the Task via Frends UI Task View or by installing the NuGet package `Frends.AzureTableStorage.DeleteTable` from the NuGet Gallery.

## Building

### Clone a copy of the repository

`git clone https://github.com/FrendsPlatform/Frends.AzureTableStorage.git`

### Build the project

`dotnet build`

### Run tests

Create a `.env` file in the `Frends.AzureTableStorage.DeleteTable.Tests` directory based on `.env.example` and configure your Azure Storage credentials.

`dotnet test`

### Create a NuGet package

`dotnet pack --configuration Release`

### StyleCop.Analyzers Version
This project uses StyleCop.Analyzers 1.2.0-beta.556, as recommended by the author, to get the latest fixes and improvements not available in the last stable release.
