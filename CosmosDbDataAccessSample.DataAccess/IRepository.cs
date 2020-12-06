// Copyright (c) Dennis Shevtsov. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// See License.txt in the project root for license information.

namespace CosmosDbDataAccessSample.DataAccess
{
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;

  using Newtonsoft.Json.Linq;

  public interface IRepository
  {
    public IAsyncEnumerable<JObject> AsEnumerableAsync(string query, string partitionKey, CancellationToken cancellationToken);

    public Task StoreAsync(string paritionKey, JObject document, CancellationToken cancellationToken);

    public Task DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken);
  }
}
