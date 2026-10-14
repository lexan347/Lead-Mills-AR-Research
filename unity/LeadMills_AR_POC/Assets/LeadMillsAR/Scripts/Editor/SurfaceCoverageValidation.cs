using System;
using UnityEditor;
using UnityEngine;

public static class SurfaceCoverageValidation
{
    [MenuItem("Lead Mills/Validate Small Patch Coverage")]
    public static void Validate()
    {
        var a=new Vector3(-1,0,-1);var b=new Vector3(1,0,-1);
        var c=new Vector3(1,0,1);var d=new Vector3(-1,0,1);
        // A 24 cm patch fully covered by two broad floor triangles. Neither
        // triangle centroid lies within the patch: the former test counted zero.
        int cells=0;
        for(int x=-3;x<3;x++)for(int z=-3;z<3;z++){
            var p=new Vector2((x+.5f)*.05f,(z+.5f)*.05f);
            if(p.sqrMagnitude>.12f*.12f)continue;
            if(SurfaceCoverage.ContainsAtPlane(a,b,c,p)||SurfaceCoverage.ContainsAtPlane(a,c,d,p))cells++;
        }
        Require(cells==16,"Broad floor triangles must support all 16 patch cells.");
        Require(SurfaceCoverage.ContainsAtPlane(c,b,a,Vector2.zero),"Triangle winding must not remove coverage.");
        Require(!SurfaceCoverage.ContainsAtPlane(a,b,c,new Vector2(2,0)),"Outside triangle must not count.");
        Require(!SurfaceCoverage.ContainsAtPlane(a+Vector3.up*.06f,b+Vector3.up*.06f,c+Vector3.up*.06f,Vector2.zero),"Surface 6 cm above the plane must not count.");
        Require(!SurfaceCoverage.ContainsAtPlane(a,a,a,Vector2.zero),"Degenerate triangle must not count.");
        Require(SurfaceCoverage.IsLevel(a,b,c),"Flat floor must pass the slope test.");
        Require(!SurfaceCoverage.IsLevel(Vector3.zero,Vector3.up,Vector3.forward),"Vertical surface must not pass the slope test.");
        var raised=new Vector3(0,.08f,0);
        Require(SurfaceCoverage.ContainsAtPlane(a+raised,b+raised,c+raised,Vector2.zero,GroundPlacementPolicy.HeightTolerance),"Grass-height variation of 8 cm must count.");
        Require(!SurfaceCoverage.ContainsAtPlane(a+Vector3.up*.2f,b+Vector3.up*.2f,c+Vector3.up*.2f,Vector2.zero,GroundPlacementPolicy.HeightTolerance),"A separate surface 20 cm away must not count.");
        var grooveA=new Vector3(-.1f,-.01f,-.1f);var grooveB=new Vector3(.1f,.01f,-.1f);var grooveC=new Vector3(.1f,-.01f,.1f);
        Require(SurfaceCoverage.IsLevel(grooveA,grooveB,grooveC,GroundPlacementPolicy.MaximumFacetSlopeDegrees) && SurfaceCoverage.ContainsAtPlane(grooveA,grooveB,grooveC,Vector2.zero,GroundPlacementPolicy.HeightTolerance),"Grooves and tile seams must be accepted.");
        var roughA=new Vector3(-.1f,-.06f,-.1f);var roughB=new Vector3(.1f,.06f,-.1f);var roughC=new Vector3(.1f,.06f,.1f);
        Require(SurfaceCoverage.IsLevel(roughA,roughB,roughC,GroundPlacementPolicy.MaximumFacetSlopeDegrees) && SurfaceCoverage.ContainsAtPlane(roughA,roughB,roughC,Vector2.zero,GroundPlacementPolicy.HeightTolerance),"Rough grass facets on a level ground estimate must be accepted.");
        Require(!SurfaceCoverage.IsLevel(Vector3.zero,Vector3.up,Vector3.forward,GroundPlacementPolicy.MaximumFacetSlopeDegrees),"Terrain tolerance must still reject walls.");
        Require(GroundPlacementPolicy.AcceptsGroundNormal(Quaternion.Euler(12,0,0)*Vector3.up),"A moderate overall 12-degree grade must be accepted.");
        Require(!GroundPlacementPolicy.AcceptsGroundNormal(Quaternion.Euler(22,0,0)*Vector3.up),"Overall 22-degree grade must be rejected.");
        Require(!GroundPlacementPolicy.EstimateChanged(Vector3.zero,Vector3.up,new Vector3(.4f,.02f,.3f),Vector3.up),"Plane boundary growth and 2 cm height refinement must not restart stability.");
        Require(GroundPlacementPolicy.EstimateChanged(Vector3.zero,Vector3.up,Vector3.up*.15f,Vector3.up),"A 15 cm height change must restart stability.");
        var hold=new GroundStabilityCheck();
        Require(!hold.Update(0,true,Vector3.zero,Vector3.zero,Vector3.up),"First supported frame must not be ready.");
        Require(!hold.Update(1,true,Vector3.zero,Vector3.zero,Vector3.up),"One second must not complete dwell.");
        Require(hold.Update(1.6f,true,Vector3.zero,Vector3.zero,Vector3.up),"Stationary supported target must become ready without any camera-travel requirement.");
        Require(hold.Update(1.7f,true,Vector3.zero,new Vector3(.4f,.02f,.3f),Vector3.up),"Harmless plane centroid change must retain readiness.");
        Require(!hold.Update(1.8f,true,new Vector3(.2f,0,0),Vector3.zero,Vector3.up),"Moving to another patch must restart dwell.");
        Require(hold.Update(3.4f,true,new Vector3(.2f,0,0),Vector3.zero,Vector3.up),"Holding the new patch must become ready.");
        Require(!hold.Update(3.5f,false,new Vector3(.2f,0,0),Vector3.zero,Vector3.up),"Losing surface support must invalidate readiness.");
        Require(!hold.Update(3.6f,true,new Vector3(.2f,0,0),Vector3.zero,Vector3.up),"Reacquisition must start a fresh dwell.");
        Require(hold.Update(5.2f,true,new Vector3(.2f,0,0),Vector3.zero,Vector3.up),"Reacquisition must recover after stable dwell.");
        Require(!hold.Update(5.3f,true,new Vector3(.2f,0,0),Vector3.up*.2f,Vector3.up),"A ground height jump must invalidate readiness.");
        Debug.Log("[Coal Shed Validation] PASS: small-patch coverage; grooves, grass-height variation, rough facets, moderate grade, centroid drift; wall, steep grade, separate-height and unstable estimate rejection; stationary readiness, target changes, support loss and recovery.");
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
