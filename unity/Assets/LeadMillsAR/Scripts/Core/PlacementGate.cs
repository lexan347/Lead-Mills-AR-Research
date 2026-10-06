namespace LeadMillsAR.Core
{
    public sealed class PlacementGate
    {
        public PlacementGate(
            double maximumHorizontalErrorMeters,
            double maximumVerticalErrorMeters,
            double maximumHeadingErrorDegrees)
        {
            MaximumHorizontalErrorMeters = maximumHorizontalErrorMeters;
            MaximumVerticalErrorMeters = maximumVerticalErrorMeters;
            MaximumHeadingErrorDegrees = maximumHeadingErrorDegrees;
        }

        public double MaximumHorizontalErrorMeters { get; }
        public double MaximumVerticalErrorMeters { get; }
        public double MaximumHeadingErrorDegrees { get; }

        public bool Allows(GeospatialStatus status)
        {
            return status.Quality == TrackingQuality.Precise
                && status.HorizontalAccuracyMeters <= MaximumHorizontalErrorMeters
                && status.VerticalAccuracyMeters <= MaximumVerticalErrorMeters
                && status.HeadingAccuracyDegrees <= MaximumHeadingErrorDegrees;
        }
    }
}
