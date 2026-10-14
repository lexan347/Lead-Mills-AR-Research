using UnityEngine;

// Prototype ground acceptance, not a measurement of surveyed grade or accuracy.
public static class GroundPlacementPolicy
{
    public const float MaximumGroundSlopeDegrees=15f;
    public const float MaximumFacetSlopeDegrees=60f;
    public const float HeightTolerance=.10f;
    public const float HeightStabilityTolerance=.08f;
    public const float NormalStabilityDegrees=8f;
    public static bool AcceptsGroundNormal(Vector3 normal) =>
        normal.sqrMagnitude>1e-8f && Vector3.Dot(normal.normalized,Vector3.up)>=Mathf.Cos(MaximumGroundSlopeDegrees*Mathf.Deg2Rad);
    public static bool EstimateChanged(Vector3 oldOrigin,Vector3 oldNormal,Vector3 newOrigin,Vector3 newNormal) =>
        Mathf.Abs(Vector3.Dot(oldOrigin-newOrigin,newNormal.normalized))>HeightStabilityTolerance ||
        Vector3.Angle(oldNormal,newNormal)>NormalStabilityDegrees;
}
