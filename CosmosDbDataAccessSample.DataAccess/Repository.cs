// Copyright (c) Dennis Shevtsov. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// See License.txt in the project root for license information.

namespace CosmosDbDataAccessSample.DataAccess
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Net;
  using System.Runtime.CompilerServices;
  using System.Threading;
  using System.Threading.Tasks;

  using Microsoft.Azure.Cosmos;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  public sealed class Repository : IRepository
  {
    private const int CosmosMaxRecordsPerRequest = 100;
    private const string CosmosDocumentsProperty = "documents";
    private const int CosmosMaxRequestTries = 5;
    private const int CosmosMillisecondsBetweenTries = 100;

    private static readonly ISet<HttpStatusCode> CosmosDeleteSuccessfullCodes = new HashSet<HttpStatusCode>
    {
      HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Accepted, HttpStatusCode.NoContent, HttpStatusCode.NotFound,
    };

    private readonly Container _container;

    public Repository(Container container) => _container = container ?? throw new ArgumentNullException(nameof(container));

    public async IAsyncEnumerable<JObject> AsEnumerableAsync(
      string query, string partitionKey, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      using (
        var feedIterator = _container.GetItemQueryStreamIterator(
          new QueryDefinition(query),
          null,
          new QueryRequestOptions
          {
            PartitionKey = new PartitionKey(partitionKey),
            MaxItemCount = Repository.CosmosMaxRecordsPerRequest,
          }))
      {
        var serializer = new JsonSerializer();

        while (feedIterator.HasMoreResults)
        {
          using (var feedResponse = await feedIterator.ReadNextAsync(cancellationToken))
          {
            if (feedResponse.IsSuccessStatusCode)
            {
              using (var streamReader = new StreamReader(feedResponse.Content))
              {
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                  var jsonResponse = serializer.Deserialize<JObject>(jsonTextReader);

                  if (jsonResponse.TryGetValue(Repository.CosmosDocumentsProperty, StringComparison.InvariantCultureIgnoreCase, out var documents))
                  {
                    foreach (var document in documents.Values<JObject>())
                    {
                      yield return document;
                    }
                  }
                }
              }
            }
          }
        }
      }
    }

    public Task StoreAsync(string paritionKey, JObject document, CancellationToken cancellationToken)
      => _container.UpsertItemAsync(document, new PartitionKey(paritionKey), new ItemRequestOptions(), cancellationToken);

    public async Task DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken)
    {
      ItemResponse<JObject> response = null;
      int tries = 1;

      while (tries < Repository.CosmosMaxRequestTries ||
             !Repository.CosmosDeleteSuccessfullCodes.Contains(response!.StatusCode))
      {
        try
        {
          response = await _container.DeleteItemAsync<JObject>(
            id,
            new PartitionKey(partitionKey),
            new ItemRequestOptions(),
            cancellationToken);
        }
        catch
        {
          await Task.Delay(Repository.CosmosMillisecondsBetweenTries);
          ++tries;
        }
      }
    }
  }
}
