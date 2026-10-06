using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LeadMillsAR.Core
{
    public interface IGeospatialAnchorProvider
    {
        string ProviderName { get; }
        GeospatialStatus Status { get; }
        event Action<GeospatialStatus> StatusChanged;

        Task StartAsync(CancellationToken cancellationToken);

        Task<GameObject> PlaceAsync(
            AnchorRecord anchor,
            GameObject prefab,
            CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);
    }
}
