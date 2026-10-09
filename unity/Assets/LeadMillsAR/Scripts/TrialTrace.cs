using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class LeadMillsBuildIdentity
{
    public string version, buildId, protocol, configuration, sourceRevision, exportedAtUtc, unityVersion;
    public int buildNumber;
}

// Private, on-device test records. No network upload and no room-media copying.
public sealed class TrialTrace
{
    [Serializable] sealed class Counter { public int lastAttempt; }
    [Serializable] sealed class Event { public string name, details, utc; public float appTime; }
    [Serializable] sealed class Record
    {
        public LeadMillsBuildIdentity build;
        public string testId, startedAtUtc, outcome;
        public int attempt;
        public List<Event> events = new List<Event>();
    }
    static LeadMillsBuildIdentity identity;
    public static string CurrentTestId { get; private set; }
    public static LeadMillsBuildIdentity Identity
    {
        get
        {
            if (identity != null) return identity;
            var asset = Resources.Load<TextAsset>("LeadMillsBuildIdentity");
            identity = asset ? JsonUtility.FromJson<LeadMillsBuildIdentity>(asset.text) :
                new LeadMillsBuildIdentity { version = "dev", buildId = "LMAR_dev_b0000", protocol = "floor-calibration-3view-v1" };
            return identity;
        }
    }
    public static string DisplayId => CurrentTestId ?? Identity.buildId + " / no trial";
    readonly Record record;
    readonly string path;
    TrialTrace(Record data, string file) { record = data; path = file; }
    public static TrialTrace Begin(string configuration)
    {
        string directory = Path.Combine(Application.persistentDataPath, "TestRuns");
        Directory.CreateDirectory(directory);
        string counterPath = Path.Combine(directory, Identity.buildId + "_attempt-counter.json");
        Counter counter = File.Exists(counterPath) ? JsonUtility.FromJson<Counter>(File.ReadAllText(counterPath)) : new Counter();
        counter.lastAttempt++;
        WriteAtomic(counterPath, JsonUtility.ToJson(counter));
        string id = Identity.buildId + "_a" + counter.lastAttempt.ToString("D3");
        CurrentTestId = id;
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
        var data = new Record { build = Identity, attempt = counter.lastAttempt, testId = id,
            startedAtUtc = DateTime.UtcNow.ToString("o"), outcome = "in-progress" };
        var trace = new TrialTrace(data, Path.Combine(directory, id + "_" + timestamp + ".json"));
        trace.Log("begin", configuration);
        Debug.Log("[Lead Mills Test ID] " + id + "; private record " + Path.GetFileName(trace.path));
        return trace;
    }
    public void Log(string name, string details)
    {
        record.events.Add(new Event { name = name, details = details, utc = DateTime.UtcNow.ToString("o"), appTime = Time.unscaledTime });
        WriteAtomic(path, JsonUtility.ToJson(record, true));
    }
    static void WriteAtomic(string destination, string content)
    {
        string temporary = destination + ".tmp";
        File.WriteAllText(temporary, content);
        if (File.Exists(destination)) File.Replace(temporary, destination, null);
        else File.Move(temporary, destination);
    }
    public void Outcome(string result) { record.outcome = result; Log("outcome", result); }
}
