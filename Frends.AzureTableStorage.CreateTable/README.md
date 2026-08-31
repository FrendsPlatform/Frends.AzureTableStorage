# Frends.AzureTableStorage.CreateTable

Creates a table in Azure Table Storage. If the table already exists, the task succeeds with Created = false, unless FailIfTableExists is set to true.

[![CreateTable_build](https://github.com/FrendsPlatform/Frends.AzureTableStorage/actions/workflows/CreateTable_test_on_main.yml/badge.svg)](https://github.com/FrendsPlatform/Frends.AzureTableStorage/actions/workflows/CreateTable_test_on_main.yml)
![Coverage](https://app-github-custom-badges.azurewebsites.net/Badge?key=FrendsPlatform/Frends.AzureTableStorage/Frends.AzureTableStorage.CreateTable|main)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

## Installing

You can install the Task via Frends UI Task View.

## Building

### Clone a copy of the repository

`git clone https://github.com/FrendsPlatform/Frends.AzureTableStorage.git`

### Build the project

`dotnet build`

### Run tests

Create a `.env` file in the `Frends.AzureTableStorage.CreateTable.Tests` directory based on `.env.example` and configure your Azure Storage credentials.

Run the tests:

`dotnet test`

### Create a NuGet package

`dotnet pack --configuration Release`

### StyleCop.Analyzers Version
This project uses StyleCop.Analyzers 1.2.0-beta.556, as recommended by the author, to get the latest fixes and improvements not available in the last stable release.
