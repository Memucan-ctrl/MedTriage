using System;
using UnityEngine;

[Serializable]
public struct PatientVitalsSnapshot
{
    public int heartRate;
    public int systolic;
    public int diastolic;
    public int spo2;
    public int respiratoryRate;
    public string rhythm;
    public string status;
}

public class PatientVitalsSimulator : MonoBehaviour
{
    public event Action<PatientVitalsSnapshot> OnVitalsChanged;
    [Header("Demo values — clinician validation required")]
    [Range(0,220)] public int heartRate = 138;
    public int systolic = 82;
    public int diastolic = 48;
    [Range(0,100)] public int spo2 = 88;
    [Range(0,50)] public int respiratoryRate = 28;
    public string rhythm = "VF / SHOCKABLE";
    public bool simulateDeterioration = true;
    [Range(0.25f,5f)] public float updateInterval = 1f;
    float timer;

    public PatientVitalsSnapshot Current => new PatientVitalsSnapshot {
        heartRate=heartRate, systolic=systolic, diastolic=diastolic,
        spo2=spo2, respiratoryRate=respiratoryRate, rhythm=rhythm,
        status=GetStatus()
    };

    void Start() => Raise();
    void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;
        if (simulateDeterioration) {
            heartRate = Mathf.Clamp(heartRate + UnityEngine.Random.Range(-2,3), 0, 220);
            spo2 = Mathf.Clamp(spo2 + UnityEngine.Random.Range(-1,2), 0, 100);
            systolic = Mathf.Clamp(systolic + UnityEngine.Random.Range(-2,3), 0, 240);
            diastolic = Mathf.Clamp(diastolic + UnityEngine.Random.Range(-1,2), 0, 160);
        }
        Raise();
    }

    public void ApplyIntervention(string actionId)
    {
        switch ((actionId ?? "").ToLowerInvariant()) {
            case "cpr": heartRate = Mathf.Max(heartRate, 70); systolic = Mathf.Max(systolic, 90); break;
            case "oxygen": spo2 = Mathf.Min(100, spo2 + 8); break;
            case "shock": rhythm = "SINUS RHYTHM"; heartRate = 92; systolic = 108; diastolic = 68; spo2 = 95; break;
            case "epinephrine": systolic = Mathf.Min(180, systolic + 15); break;
        }
        Raise();
    }

    public string GetStatus()
    {
        if (heartRate <= 0 || spo2 < 80 || systolic < 70) return "CRITICAL — ACT NOW";
        if (spo2 < 92 || systolic < 90 || heartRate > 120) return "UNSTABLE — REASSESS";
        return "STABLE — CONTINUE CARE";
    }

    public void Raise() => OnVitalsChanged?.Invoke(Current);
}
