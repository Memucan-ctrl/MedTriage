using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MedTriageBootstrap : MonoBehaviour
{
    [Header("Challenge experience")]
    public bool autoStartScenario = true;
    public bool guidedPractice = true;
    [Header("Comfort defaults")]
    public bool teleportEnabled = true;
    public bool snapTurnEnabled = true;
    public bool movementVignetteEnabled = true;
    public bool captionsEnabled = true;
    public float masterVolume = .8f;

    PatientVitalsSimulator vitals;
    ScenarioManager scenario;
    AudioSource monitorAudio;
    Text hrText, bpText, spo2Text, rrText, rhythmText, statusText;
    Text objectiveText, timerText, feedbackText, captionsText, debriefText;
    GameObject debriefPanel;
    float beepTimer;
    readonly Color navy = new Color32(10,22,37,245);
    readonly Color surface = new Color32(19,38,58,245);
    readonly Color cyan = new Color32(75,210,225,255);
    readonly Color amber = new Color32(255,190,76,255);
    readonly Color risk = new Color32(255,106,97,255);
    readonly Color good = new Color32(98,210,151,255);

    void Awake()
    {
        Application.targetFrameRate = 72;
        QualitySettings.vSyncCount = 0;
        LoadPreferences();
        vitals = FindAnyObjectByType<PatientVitalsSimulator>();
        if (!vitals) vitals = gameObject.AddComponent<PatientVitalsSimulator>();
        scenario = FindAnyObjectByType<ScenarioManager>();
        if (!scenario) scenario = gameObject.AddComponent<ScenarioManager>();
        scenario.mode = guidedPractice ? ScenarioManager.RunMode.GuidedPractice : ScenarioManager.RunMode.Assessment;
        monitorAudio = gameObject.AddComponent<AudioSource>();
        monitorAudio.spatialBlend = 1f; monitorAudio.volume = masterVolume * .22f; monitorAudio.playOnAwake = false;
        EnsurePreviewCameraAndAudio();
        BuildExperienceUI();
        vitals.OnVitalsChanged += RefreshVitals;
        scenario.OnStepChanged += RefreshObjective;
        scenario.OnActionFeedback += ShowFeedback;
        scenario.OnScenarioCompleted += ShowDebrief;
    }

    IEnumerator Start()
    {
        yield return null;
        RefreshVitals(vitals.Current);
        if (autoStartScenario) scenario.StartScenario();
        ShowCaption("Simulation ready. Follow the objective panel.");
    }

    void OnDestroy()
    {
        if (vitals != null) vitals.OnVitalsChanged -= RefreshVitals;
        if (scenario != null) { scenario.OnStepChanged -= RefreshObjective; scenario.OnActionFeedback -= ShowFeedback; scenario.OnScenarioCompleted -= ShowDebrief; }
    }

    void Update()
    {
        if (scenario && scenario.IsRunning && timerText) timerText.text = FormatTime(scenario.ScenarioElapsed);
        beepTimer -= Time.deltaTime;
        if (beepTimer <= 0 && vitals && vitals.heartRate > 0) {
            beepTimer = 60f / Mathf.Clamp(vitals.heartRate, 35, 200);
            monitorAudio.pitch = Mathf.Lerp(.82f, 1.25f, Mathf.InverseLerp(40,180,vitals.heartRate));
            monitorAudio.PlayOneShot(CreateTone(0.055f, 880f), masterVolume * .2f);
        }
        // Editor/device-simulator validation shortcuts via the active Input System.
        var keyboard = Keyboard.current;
        if (keyboard == null || scenario == null) return;
        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) scenario.SubmitAction("assess");
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) scenario.SubmitAction("call_help");
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) scenario.SubmitAction("check_pulse");
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) scenario.SubmitAction("start_cpr");
        if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) scenario.SubmitAction("attach_defib");
        if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) scenario.SubmitAction("shock");
        if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame) scenario.SubmitAction("reassess");
    }

    void EnsurePreviewCameraAndAudio()
    {
        Camera camera = null;
        var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        foreach (var candidate in cameras) if (candidate && candidate.enabled && candidate.gameObject.activeInHierarchy) { camera = candidate; break; }
        if (!camera)
        {
            var anchor = FindAnchor();
            var cameraObject = new GameObject("MedTriage_DesktopPreviewCamera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 250f;
            camera.fieldOfView = 60f;
            var target = anchor ? anchor.position + Vector3.up * 1.3f : Vector3.up * 1.3f;
            var forward = anchor ? anchor.forward : Vector3.forward;
            var right = anchor ? anchor.right : Vector3.right;
            cameraObject.transform.position = target - forward * 2.8f + right * 1.4f + Vector3.up * .15f;
            cameraObject.transform.LookAt(target);
            Debug.Log("[MedTriage] Added desktop preview camera because the XR rig has no active camera.");
        }
        if (!FindAnyObjectByType<AudioListener>()) camera.gameObject.AddComponent<AudioListener>();
        if (camera.gameObject.name == "MedTriage_DesktopPreviewCamera" && !camera.GetComponent<MedTriageDesktopFlyCamera>())
            camera.gameObject.AddComponent<MedTriageDesktopFlyCamera>();
    }

    void BuildExperienceUI()
    {
        Transform anchor = FindAnchor();
        var monitor = new GameObject("vitals_monitor");
        monitor.transform.SetParent(anchor, false);
        monitor.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        monitor.transform.localRotation = Quaternion.identity;
        var canvas = CreateCanvas(monitor.transform, "VitalsCanvas", new Vector2(900,520), .00115f);
        AddPanel(canvas.transform, navy, Vector2.zero, new Vector2(900,520));
        AddText(canvas.transform,"Header","PATIENT MONITOR  •  LIVE",32,FontStyle.Bold,cyan,new Vector2(0,220),new Vector2(820,55),TextAnchor.MiddleLeft);
        hrText=AddMetric(canvas.transform,"HR","---","bpm",new Vector2(-265,95),cyan);
        spo2Text=AddMetric(canvas.transform,"SpO₂","---","%",new Vector2(0,95),good);
        rrText=AddMetric(canvas.transform,"RESP","---","/min",new Vector2(265,95),amber);
        bpText=AddMetric(canvas.transform,"NIBP","---/---","mmHg",new Vector2(-180,-75),Color.white);
        rhythmText=AddMetric(canvas.transform,"RHYTHM","---","",new Vector2(180,-75),cyan);
        statusText=AddText(canvas.transform,"Status","◆ WAITING FOR DATA",28,FontStyle.Bold,amber,new Vector2(0,-205),new Vector2(820,60),TextAnchor.MiddleCenter);

        var objectiveRoot = new GameObject("Scenario_Objective_Panel");
        objectiveRoot.transform.SetParent(anchor, false);
        objectiveRoot.transform.localPosition = new Vector3(-1.15f,1.55f,.12f);
        objectiveRoot.transform.localRotation = Quaternion.Euler(0,12,0);
        var objectiveCanvas=CreateCanvas(objectiveRoot.transform,"ObjectiveCanvas",new Vector2(720,410),.0012f);
        AddPanel(objectiveCanvas.transform,surface,Vector2.zero,new Vector2(720,410));
        AddText(objectiveCanvas.transform,"Kicker","ACTIVE OBJECTIVE",24,FontStyle.Bold,cyan,new Vector2(0,160),new Vector2(650,42),TextAnchor.MiddleLeft);
        objectiveText=AddText(objectiveCanvas.transform,"Objective","Preparing scenario…",34,FontStyle.Bold,Color.white,new Vector2(0,55),new Vector2(650,150),TextAnchor.MiddleLeft);
        timerText=AddText(objectiveCanvas.transform,"Timer","00:00",34,FontStyle.Bold,amber,new Vector2(-230,-125),new Vector2(190,55),TextAnchor.MiddleLeft);
        feedbackText=AddText(objectiveCanvas.transform,"Feedback","Guided practice",24,FontStyle.Normal,good,new Vector2(70,-125),new Vector2(390,55),TextAnchor.MiddleLeft);

        var captionRoot=new GameObject("Accessible_Captions"); captionRoot.transform.SetParent(anchor,false);
        captionRoot.transform.localPosition=new Vector3(0,.65f,.06f);
        var captionCanvas=CreateCanvas(captionRoot.transform,"CaptionCanvas",new Vector2(900,110),.0011f);
        AddPanel(captionCanvas.transform,new Color32(0,0,0,220),Vector2.zero,new Vector2(900,110));
        captionsText=AddText(captionCanvas.transform,"Caption","",26,FontStyle.Bold,Color.white,Vector2.zero,new Vector2(820,82),TextAnchor.MiddleCenter);
        captionRoot.SetActive(captionsEnabled);

        debriefPanel=new GameObject("Scenario_Debrief_Panel"); debriefPanel.transform.SetParent(anchor,false);
        debriefPanel.transform.localPosition=new Vector3(1.2f,1.45f,.15f); debriefPanel.transform.localRotation=Quaternion.Euler(0,-12,0);
        var debriefCanvas=CreateCanvas(debriefPanel.transform,"DebriefCanvas",new Vector2(820,620),.00115f);
        AddPanel(debriefCanvas.transform,navy,Vector2.zero,new Vector2(820,620));
        AddText(debriefCanvas.transform,"Header","SESSION DEBRIEF",34,FontStyle.Bold,cyan,new Vector2(0,265),new Vector2(740,55),TextAnchor.MiddleLeft);
        debriefText=AddText(debriefCanvas.transform,"Results","",24,FontStyle.Normal,Color.white,new Vector2(0,-15),new Vector2(740,470),TextAnchor.UpperLeft);
        debriefPanel.SetActive(false);
    }

    Transform FindAnchor()
    {
        var marker = GameObject.Find("Future_PatientMonitor_Position");
        if (marker) return marker.transform;
        var cardiac = GameObject.Find("CardiacRoom_Reserved");
        if (cardiac) return cardiac.transform;
        return transform;
    }

    Canvas CreateCanvas(Transform parent,string name,Vector2 size,float scale)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); go.transform.SetParent(parent,false);
        var c=go.GetComponent<Canvas>(); c.renderMode=RenderMode.WorldSpace; c.sortingOrder=20;
        var rt=(RectTransform)go.transform; rt.sizeDelta=size; rt.localScale=Vector3.one*scale;
        var scaler=go.GetComponent<CanvasScaler>(); scaler.dynamicPixelsPerUnit=12;
        return c;
    }
    Image AddPanel(Transform p,Color color,Vector2 pos,Vector2 size) { var g=new GameObject("Surface",typeof(RectTransform),typeof(Image)); g.transform.SetParent(p,false); var rt=(RectTransform)g.transform; rt.anchoredPosition=pos; rt.sizeDelta=size; var im=g.GetComponent<Image>(); im.color=color; return im; }
    Text AddText(Transform p,string name,string value,int size,FontStyle style,Color color,Vector2 pos,Vector2 box,TextAnchor anchor)
    { var g=new GameObject(name,typeof(RectTransform),typeof(Text)); g.transform.SetParent(p,false); var rt=(RectTransform)g.transform; rt.anchoredPosition=pos; rt.sizeDelta=box; var t=g.GetComponent<Text>(); t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.text=value; t.fontSize=size; t.fontStyle=style; t.color=color; t.alignment=anchor; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate; return t; }
    Text AddMetric(Transform p,string label,string value,string unit,Vector2 pos,Color color)
    { var box=new GameObject(label+"_Card",typeof(RectTransform),typeof(Image)); box.transform.SetParent(p,false); var rt=(RectTransform)box.transform; rt.anchoredPosition=pos; rt.sizeDelta=new Vector2(235,145); box.GetComponent<Image>().color=surface; AddText(box.transform,"Label",label,22,FontStyle.Bold,new Color(1,1,1,.7f),new Vector2(0,45),new Vector2(195,32),TextAnchor.MiddleLeft); var v=AddText(box.transform,"Value",value,42,FontStyle.Bold,color,new Vector2(-18,-8),new Vector2(160,62),TextAnchor.MiddleLeft); AddText(box.transform,"Unit",unit,18,FontStyle.Normal,new Color(1,1,1,.7f),new Vector2(75,-20),new Vector2(65,30),TextAnchor.MiddleLeft); return v; }

    void RefreshVitals(PatientVitalsSnapshot v)
    {
        if (!hrText) return; hrText.text=v.heartRate.ToString(); bpText.text=v.systolic+"/"+v.diastolic; spo2Text.text=v.spo2.ToString(); rrText.text=v.respiratoryRate.ToString(); rhythmText.text=v.rhythm;
        statusText.text=(v.status.StartsWith("CRITICAL")?"▲ ":v.status.StartsWith("UNSTABLE")?"◆ ":"● ")+v.status;
        statusText.color=v.status.StartsWith("CRITICAL")?risk:v.status.StartsWith("UNSTABLE")?amber:good;
    }
    void RefreshObjective(ScenarioStepData step,int index,int count)
    { objectiveText.text=(index+1)+" / "+count+"\n"+step.title+"\n<size=22>"+step.instruction+"</size>"; feedbackText.text=scenario.mode==ScenarioManager.RunMode.GuidedPractice?"GUIDED • HINTS ON":"ASSESSMENT • HINTS OFF"; feedbackText.color=scenario.mode==ScenarioManager.RunMode.GuidedPractice?good:amber; ShowCaption(step.title+". "+step.instruction); }
    void ShowFeedback(string message,bool success) { feedbackText.text=(success?"✓ ":"! ")+message; feedbackText.color=success?good:amber; monitorAudio.PlayOneShot(CreateTone(.09f,success?1046f:260f),masterVolume*.35f); ShowCaption(message); }
    void ShowDebrief(IReadOnlyList<ScenarioScoreEntry> log)
    { var sb=new StringBuilder(); int pass=0; foreach(var e in log){ if(e.passed)pass++; sb.Append(e.passed?"✓ ":"✕ ").Append(e.title).Append("  •  ").Append(e.elapsedSeconds.ToString("0.0")).Append("s  •  ").Append(e.note).AppendLine(); } sb.AppendLine().Append("SCORE  ").Append(pass).Append(" / ").Append(log.Count).Append("   •   TOTAL  ").Append(FormatTime(scenario.ScenarioElapsed)); debriefText.text=sb.ToString(); debriefPanel.SetActive(true); ExportSession(log); ShowCaption("Scenario complete. Review your debrief."); }
    void ShowCaption(string text) { if(captionsEnabled && captionsText) captionsText.text=text; }
    string FormatTime(float s) => Mathf.FloorToInt(s/60).ToString("00")+":"+Mathf.FloorToInt(s%60).ToString("00");

    AudioClip CreateTone(float duration,float frequency)
    { int rate=22050, count=Mathf.CeilToInt(rate*duration); var data=new float[count]; for(int i=0;i<count;i++){ float env=1f-(float)i/count; data[i]=Mathf.Sin(2*Mathf.PI*frequency*i/rate)*env*.18f; } var clip=AudioClip.Create("MedTriageTone",count,1,rate,false); clip.SetData(data,0); return clip; }
    void ExportSession(IReadOnlyList<ScenarioScoreEntry> log)
    { try { string dir=Path.Combine(Application.persistentDataPath,"MedTriageSessions"); Directory.CreateDirectory(dir); string path=Path.Combine(dir,"session_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+".csv"); var sb=new StringBuilder("step_id,title,passed,elapsed_seconds,note\n"); foreach(var e in log) sb.Append(E(e.stepId)).Append(',').Append(E(e.title)).Append(',').Append(e.passed).Append(',').Append(e.elapsedSeconds.ToString("0.0")).Append(',').Append(E(e.note)).AppendLine(); File.WriteAllText(path,sb.ToString()); Debug.Log("[MedTriage] Session exported: "+path); } catch(Exception ex){ Debug.LogWarning("[MedTriage] Export failed: "+ex.Message); } }
    string E(string s)=>"\""+(s??"").Replace("\"","\"\"")+"\"";
    void LoadPreferences()
    { teleportEnabled=PlayerPrefs.GetInt("MT_Teleport",1)==1; snapTurnEnabled=PlayerPrefs.GetInt("MT_SnapTurn",1)==1; movementVignetteEnabled=PlayerPrefs.GetInt("MT_Vignette",1)==1; captionsEnabled=PlayerPrefs.GetInt("MT_Captions",1)==1; masterVolume=PlayerPrefs.GetFloat("MT_Volume",.8f); }
    public void SavePreferences()
    { PlayerPrefs.SetInt("MT_Teleport",teleportEnabled?1:0); PlayerPrefs.SetInt("MT_SnapTurn",snapTurnEnabled?1:0); PlayerPrefs.SetInt("MT_Vignette",movementVignetteEnabled?1:0); PlayerPrefs.SetInt("MT_Captions",captionsEnabled?1:0); PlayerPrefs.SetFloat("MT_Volume",masterVolume); PlayerPrefs.Save(); }
}
