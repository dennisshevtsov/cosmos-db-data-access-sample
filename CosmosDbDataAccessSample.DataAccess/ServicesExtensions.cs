// Copyright (c) Dennis Shevtsov. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// See License.txt in the project root for license information.

namespace CosmosDbDataAccessSample.DataAccess
{
  using System;

  using Microsoft.Azure.Cosmos;
  using Microsoft.Extensions.DependencyInjection;

  public static class ServicesExtensions
  {
    public static IServiceCollection AddDataAccess(
      this IServiceCollection services,
      string accountEndpoint,
      string accountKey,
      string databaseId,
      string containerId)
    {
      if (services == null)
      {
        throw new ArgumentNullException(nameof(services));
      }

      if (string.IsNullOrWhiteSpace(accountEndpoint))
      {
        throw new ArgumentException($"Argument {nameof(accountEndpoint)} cannot be empty.");
      }

      if (string.IsNullOrWhiteSpace(accountKey))
      {
        throw new ArgumentException($"Argument {nameof(accountKey)} cannot be empty.");
      }

      if (string.IsNullOrWhiteSpace(databaseId))
      {
        throw new ArgumentException($"Argument {nameof(databaseId)} cannot be empty.");
      }

      if (string.IsNullOrWhiteSpace(containerId))
      {
        throw new ArgumentException($"Argument {nameof(containerId)} cannot be empty.");
      }

      services.AddScoped(provider =>
        new CosmosClient(
          accountEndpoint,
          accountKey,
          new CosmosClientOptions
          {
            SerializerOptions = new CosmosSerializationOptions
            {
              PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            },
          }));
      services.AddScoped(provider =>
        provider.GetRequiredService<CosmosClient>().GetContainer(databaseId, containerId));

      services.AddScoped<IRepository, Repository>();

      return services;
    }
  }
}
