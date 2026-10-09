using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildTraceability
{
    [System.Serializable] sealed class Configuration { public string version, protocol, configuration; }
    [System.Serializable] sealed class Sequence { public int buildNumber; }
    public static string PrepareExport()
    {
        string project = Directory.GetParent(Application.dataPath).FullName;
        string builds = Path.Combine(project, "Builds");
        Directory.CreateDirectory(builds);
        var config = JsonUtility.FromJson<Configuration>(File.ReadAllText(Path.Combine(Application.dataPath, "LeadMillsAR/TestConfiguration.json")));
        if (config == null || !System.Text.RegularExpressions.Regex.IsMatch(config.version ?? "", @"^\d+\.\d+\.\d+$"))
            throw new System.InvalidOperationException("TestConfiguration.json version must be numeric major.minor.patch.");
        string counterPath = Path.Combine(builds, "lead-mills-build-sequence.json");
        var sequence = File.Exists(counterPath) ? JsonUtility.FromJson<Sequence>(File.ReadAllText(counterPath)) : new Sequence();
        sequence.buildNumber++;
        File.WriteAllText(counterPath, JsonUtility.ToJson(sequence, true));
        string id = "LMAR_v" + config.version + "_b" + sequence.buildNumber.ToString("D4");
        string revisionPath = Path.Combine(builds, "source-revision.txt");
        var identity = new LeadMillsBuildIdentity {
            version = config.version, buildNumber = sequence.buildNumber, buildId = id,
            protocol = config.protocol, configuration = config.configuration,
            sourceRevision = File.Exists(revisionPath) ? File.ReadAllText(revisionPath).Trim() : "not-recorded",
            exportedAtUtc = System.DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion
        };
        string resourceDirectory = Path.Combine(Application.dataPath, "LeadMillsAR/Resources");
        Directory.CreateDirectory(resourceDirectory);
        File.WriteAllText(Path.Combine(resourceDirectory, "LeadMillsBuildIdentity.json"), JsonUtility.ToJson(identity, true));
        AssetDatabase.ImportAsset("Assets/LeadMillsAR/Resources/LeadMillsBuildIdentity.json", ImportAssetOptions.ForceUpdate);
        PlayerSettings.bundleVersion = config.version;
        PlayerSettings.iOS.buildNumber = sequence.buildNumber.ToString();
        string output = Path.Combine(builds, id + "_" + System.DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"));
        Directory.CreateDirectory(output);
        // BuildPipeline clears a fresh iOS export directory; save the manifest after it finishes.
        Debug.Log("[Lead Mills Test ID] Export " + id + "; source " + identity.sourceRevision);
        return output;
    }
    public static void SaveManifest(string output)
    {
        string json = File.ReadAllText(Path.Combine(Application.dataPath, "LeadMillsAR/Resources/LeadMillsBuildIdentity.json"));
        var identity = JsonUtility.FromJson<LeadMillsBuildIdentity>(json);
        File.WriteAllText(Path.Combine(output, identity.buildId + "_build-manifest.json"), json);
    }
}
