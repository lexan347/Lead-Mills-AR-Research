using UnityEngine;

// Stability is local surface evidence over time. Camera travel is a scanning
// suggestion, not a prerequisite that conflicts with holding the target steady.
public sealed class GroundStabilityCheck
{
    public const float DwellSeconds=1.5f;
    public const float TargetMovementTolerance=.08f;
    bool started;
    float since;
    Vector3 point,planeOrigin,planeNormal;
    public void Reset(){started=false;}
    public float Elapsed(float now)=>started?Mathf.Max(0,now-since):0;
    public bool Update(float now,bool supported,Vector3 target,Vector3 origin,Vector3 normal)
    {
        if(!supported){Reset();return false;}
        if(!started || Vector3.Distance(point,target)>TargetMovementTolerance ||
            GroundPlacementPolicy.EstimateChanged(planeOrigin,planeNormal,origin,normal))
        {
            started=true;since=now;point=target;planeOrigin=origin;planeNormal=normal;
        }
        return Elapsed(now)>=DwellSeconds;
    }
}
