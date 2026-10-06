using System;

namespace LeadMillsAR.Core
{
    public enum TrackingQuality
    {
        Unavailable,
        Initializing,
        Coarse,
        Precise,
        Lost,
        Failed
    }

    [Serializable]
    public readonly struct GeospatialStatus
    {
        public GeospatialStatus(
            string provider,
            TrackingQuality quality,
            double horizontalAccuracyMeters,
            double verticalAccuracyMeters,
            double headingAccuracyDegrees,
            string detail)
        {
            Provider = provider;
            Quality = quality;
            HorizontalAccuracyMeters = horizontalAccuracyMeters;
            VerticalAccuracyMeters = verticalAccuracyMeters;
            HeadingAccuracyDegrees = headingAccuracyDegrees;
            Detail = detail;
        }

        public string Provider { get; }
        public TrackingQuality Quality { get; }
        public double HorizontalAccuracyMeters { get; }
        public double VerticalAccuracyMeters { get; }
        public double HeadingAccuracyDegrees { get; }
        public string Detail { get; }
    }
}
