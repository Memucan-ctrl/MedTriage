using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class ScenarioStepData {
    public string id; public string title; [TextArea] public string instruction; public string interventionId;
    public float targetSeconds = 30f; public bool critical;
}
[Serializable] public class ScenarioScoreEntry {
    public string stepId; public string title; public bool passed; public float elapsedSeconds; public string note;
}

public class ScenarioManager : MonoBehaviour
{
    public enum RunMode { GuidedPractice, Assessment }
    public event Action<ScenarioStepData,int,int> OnStepChanged;
    public event Action<IReadOnlyList<ScenarioScoreEntry>> OnScenarioCompleted;
    public event Action<string,bool> OnActionFeedback;
    public RunMode mode = RunMode.GuidedPractice;
    public List<ScenarioStepData> steps = new List<ScenarioStepData>();
    public List<ScenarioScoreEntry> scoringLog = new List<ScenarioScoreEntry>();
    public int CurrentIndex { get; private set; } = -1;
    public bool IsRunning { get; private set; }
    public float ScenarioElapsed { get; private set; }
    float stepStarted;

    public ScenarioStepData CurrentStep => CurrentIndex >= 0 && CurrentIndex < steps.Count ? steps[CurrentIndex] : null;

    void Awake() { if (steps.Count == 0) BuildChallengeDemo(); }
    void Update() { if (IsRunning) ScenarioElapsed += Time.deltaTime; }

    public void StartScenario()
    {
        scoringLog.Clear(); ScenarioElapsed = 0; CurrentIndex = 0; stepStarted = 0; IsRunning = steps.Count > 0;
        if (IsRunning) OnStepChanged?.Invoke(CurrentStep, CurrentIndex, steps.Count);
    }

    public bool SubmitAction(string actionId)
    {
        if (!IsRunning || CurrentStep == null) return false;
        bool pass = string.Equals(CurrentStep.id, actionId, StringComparison.OrdinalIgnoreCase);
        OnActionFeedback?.Invoke(pass ? "Correct — continue" : (mode == RunMode.GuidedPractice ? "Not yet — review the current objective" : "Action recorded"), pass);
        if (!pass) return false;
        float elapsed = ScenarioElapsed - stepStarted;
        scoringLog.Add(new ScenarioScoreEntry { stepId=CurrentStep.id, title=CurrentStep.title, passed=true, elapsedSeconds=elapsed, note=elapsed <= CurrentStep.targetSeconds ? "On target" : "Completed late" });
        var vitals = FindAnyObjectByType<PatientVitalsSimulator>();
        if (vitals && !string.IsNullOrEmpty(CurrentStep.interventionId)) vitals.ApplyIntervention(CurrentStep.interventionId);
        CurrentIndex++;
        if (CurrentIndex >= steps.Count) { IsRunning=false; OnScenarioCompleted?.Invoke(scoringLog); return true; }
        stepStarted = ScenarioElapsed; OnStepChanged?.Invoke(CurrentStep, CurrentIndex, steps.Count); return true;
    }

    public void EndScenario(string note="Ended by facilitator")
    {
        if (!IsRunning) return;
        if (CurrentStep != null) scoringLog.Add(new ScenarioScoreEntry { stepId=CurrentStep.id, title=CurrentStep.title, passed=false, elapsedSeconds=ScenarioElapsed-stepStarted, note=note });
        IsRunning=false; OnScenarioCompleted?.Invoke(scoringLog);
    }

    void BuildChallengeDemo()
    {
        steps.Add(new ScenarioStepData{id="assess",title="Assess responsiveness",instruction="Check responsiveness and immediate danger.",targetSeconds=10,critical=true});
        steps.Add(new ScenarioStepData{id="call_help",title="Activate emergency response",instruction="Call for help and request the resuscitation team.",targetSeconds=10,critical=true});
        steps.Add(new ScenarioStepData{id="check_pulse",title="Check breathing and pulse",instruction="Assess breathing and pulse without delaying CPR.",targetSeconds=10,critical=true});
        steps.Add(new ScenarioStepData{id="start_cpr",title="Start high-quality CPR",instruction="Begin compressions and minimise interruptions.",interventionId="cpr",targetSeconds=15,critical=true});
        steps.Add(new ScenarioStepData{id="attach_defib",title="Attach defibrillator",instruction="Apply pads and prepare rhythm analysis.",targetSeconds=30,critical=true});
        steps.Add(new ScenarioStepData{id="shock",title="Deliver indicated shock",instruction="Clear the patient and deliver the indicated shock.",interventionId="shock",targetSeconds=30,critical=true});
        steps.Add(new ScenarioStepData{id="reassess",title="Reassess and continue care",instruction="Resume care, reassess vitals, and communicate the plan.",targetSeconds=30});
    }
}
