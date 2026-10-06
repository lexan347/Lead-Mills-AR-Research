using System;

namespace LeadMillsAR.Core
{
    [Serializable]
    public sealed class AnchorRecord
    {
        public string id = string.Empty;
        public string label = string.Empty;
        public string assetId = string.Empty;
        public bool hasSurveyedPosition;
        public double latitude;
        public double longitude;
        public double altitudeMeters;
        public string altitudeReference = "unresolved";
        public double headingDegreesTrue;
        public double horizontalUncertaintyMeters;
        public double verticalUncertaintyMeters;
        public double headingUncertaintyDegrees;
    }
}
