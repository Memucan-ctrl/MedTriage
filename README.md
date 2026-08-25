# MedTriage: VR Medical Training Simulation 🏥🥽

> Multiplayer VR simulation that trains first responders on mass-casualty triage and cardiac resuscitation. Patient vitals degrade in real time, so slow decisions cost lives. Built for the **IEEE Metaverse Grand Challenge 2026**.

Runs standalone on **Meta Quest 3** (Android, ARM64, Vulkan, IL2CPP).

## Features

### 🏨 Hospital Environment
- Custom hospital site modeled in **Blender**, exported as a URP-ready FBX (`Hospital_Site.fbx`)
- 15 reusable furniture wrapper prefabs with collision geometry and bottom pivots to prevent clipping
- 58 placed furnishing objects: waiting bay, reception, IV poles, exterior plaza
- Fixed geometry marked `Static` for draw-call batching; interactive props (beds, wheelchairs, IV poles) stay dynamic

### ❤️ Cardiac Resuscitation MVP (`Cardiac_MVP.unity`)
The full resuscitation training loop for a patient in cardiac arrest:
- Patient state machine (`PatientPresence.cs`, `PatientVitalsMotion.cs`, `PatientMonitorDisplay.cs`) drives visual status, animations (e.g. convulsions) and the monitor rhythm: Ventricular Fibrillation, Asystole, ROSC
- Arrest loop (`ArrestLoopBridge.cs`) connects shocks, chest compressions and epinephrine counters to the state machine:
  - Starts in **Ventricular Fibrillation**. 90s without a successful shock → **Asystole** (flatline tone)
  - **ROSC** needs **2 shocks, 20 compressions and epinephrine**
- Physical VR interactions: defibrillator pad placement, pulse oximeter probe attachment, IV sites, syringe draw and injection

### 👥 Triage Scoring
- Scores prioritization accuracy, team communication and protocol adherence under time pressure

## Tech Stack

| Layer | Tech |
| --- | --- |
| Engine | Unity 6 (6000.5.x) |
| Render pipeline | Universal Render Pipeline (URP 17.5.0) |
| XR | XR Interaction Toolkit 3.5.1 · XR Hands 1.8.0 · OpenXR 1.17.1 |
| Language | C# |
| 3D assets | Blender (custom hospital + props) |
| Target | Meta Quest 3, Android ARM64, Vulkan, IL2CPP |

## Project Structure

```
Assets/
├── MedTriage/           # Core gameplay scripts, prefabs, scenes
├── Scenes/              # Hospital_Integration, Cardiac_MVP
├── Hospital_Site.fbx    # Custom Blender hospital export
├── AssetsHospitalKit/   # Hospital prop kit
└── XR/ , XRI/           # XR rig and interaction configuration
```

## Roadmap (in active development)
- [ ] Audio mixing pass: flatline tone overlaps the defibrillator charge sound
- [ ] Physical syringe mechanics: joystick draw from ampule, site-specific injection (replacing the GUI shortcut)
- [ ] Hand alignment and grab physics polish: drop behavior, proper syringe grip pose

## Author
**Memucan Kiprono**, Nairobi, Kenya
[GitHub](https://github.com/Memucan-ctrl) · [LinkedIn](https://www.linkedin.com/in/memucan-leitich-81b30837a/)
