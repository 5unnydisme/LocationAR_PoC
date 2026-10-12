# LocationAR_PoC: Unity Learning Project

**A functional prototype, not production-ready.** Built with a tech stack that fits my existing experience working with Unity.

## Hand-Off Summary

This repository is organized around one active Unity scene and a small set of scene-wired scripts that control GPS, road lookup, project cards, image capture, CV review, and grievance submission. Legacy scripts are kept in the tree for reference, but they are not part of the current flow and should be treated as historical context unless explicitly reactivated.

## Current Active Setup

- Active scene: [Assets/Scenes/RevisedLocationResolver.unity](Assets/Scenes/RevisedLocationResolver.unity)
- Active data loader: [Assets/Scripts/LocationJSONLoader.cs](Assets/Scripts/LocationJSONLoader.cs)
- Active JSON dataset: [Assets/Resources/test.json](Assets/Resources/test.json)
- Legacy data path, retained for reference only: [Assets/Scripts/InfrastructureAssetDatabase.cs](Assets/Scripts/InfrastructureAssetDatabase.cs) + [Assets/Resources/InfrastructureAssets.json](Assets/Resources/InfrastructureAssets.json)

## Script Inventory

### Core flow scripts

- [MainUIFlowController.cs](Assets/Scripts/MainUIFlowController.cs) - owns the main UI state machine, panel switching, flow buttons, and transitions between GPS lookup, road success/failure, and report submission.
- [LocationServiceManager.cs](Assets/Scripts/LocationServiceManager.cs) - starts GPS, handles Android permission flow, captures snapshots, and applies the sample/accuracy gate before the road lookup runs.
- [GoogleMapsServices.cs](Assets/Scripts/GoogleMapsServices.cs) - resolves the road name from coordinates through the Maps API.
- [LocationJSONLoader.cs](Assets/Scripts/LocationJSONLoader.cs) - loads the active JSON dataset and matches the road name to local project data.
- [PanelPopulator.cs](Assets/Scripts/PanelPopulator.cs) - fills the road/project info panel from the matched location data.
- [ProjectCardController.cs](Assets/Scripts/ProjectCardController.cs) - drives the individual project card presentation in the road project list.
- [SharedCameraManager.cs](Assets/Scripts/SharedCameraManager.cs) - handles camera capture, preview image setup, and image cleanup.
- [ModelDropdownController.cs](Assets/Scripts/ModelDropdownController.cs) - populates the issue-type dropdown, tracks the selected model, and enables the Analyse button only after a valid selection.
- [CVResultPopulator.cs](Assets/Scripts/CVResultPopulator.cs) - renders the CV result panel for the review flow.
- [ReportSubmissionManager.cs](Assets/Scripts/ReportSubmissionManager.cs) - prepares the CV submission payload and controls the CV-side submission step.
- [GrievanceSubmissionManager.cs](Assets/Scripts/GrievanceSubmissionManager.cs) - posts the final grievance payload.
- [DebugOverlayController.cs](Assets/Scripts/DebugOverlayController.cs) - mirrors debug and test-mode information on device.
- [FlowDebugOverlay.cs](Assets/Scripts/FlowDebugOverlay.cs) - shows flow-specific state and diagnostic messages.

### Helper UI scripts

- [LoadingDotsCircleLayout.cs](Assets/Scripts/LoadingDotsCircleLayout.cs) - positions loading dots in a circular layout.
- [LoadingCircleStepAnimator.cs](Assets/Scripts/LoadingCircleStepAnimator.cs) - animates a dotted loading indicator by stepping the highlight between dots.

### Legacy or reference-only scripts

- [PanelManager.cs](Assets/Scripts/PanelManager.cs) - older panel/state controller kept as commented historical reference, not the active flow.
- [PanelManager_BackUp.cs](Assets/Scripts/PanelManager_BackUp.cs) - earlier backup version of the panel manager, retained for history only.
- [LoadingCircleAnimation.cs](Assets/Scripts/LoadingCircleAnimation.cs) - older spinner variant that is still present for reference, but the current loading indicator uses the dot-based helper scripts above.
- [InfrastructureAssetDatabase.cs](Assets/Scripts/InfrastructureAssetDatabase.cs) - legacy dataset path kept for comparison with the active JSON loader.

## Public Repo Notes

- Do not commit real API keys or backend URLs.
- Keep environment-specific endpoint values out of the public README.
- If a teammate needs local overrides, store them in a gitignored secrets file or device-local settings instead of checking them in.

## Git Workflow

### Main branch
- `main` is always the stable branch.
- Only tested and reviewed work should be merged into `main`.

### Feature branches
- Create a feature branch from `main` for each focused task.
- Branch name format: `feature/<short-name>`.
- Example (first feature): `feature/failure-states`.

### Daily flow
1. Start from latest `main`.
2. Create a feature branch.
3. Commit small logical changes.
4. Push branch to origin.
5. Open a Pull Request to `main`.
6. Review and test.
7. Merge when stable.

## First Feature Branch: Failure States

Use branch `feature/failure-states` for work related to failure-state handling, including:
- No road detected
- Road not in government dataset
- Multiple matching projects
- CV service failure fallback to complaint submission

Use the exact user-facing messages required by project instructions.

## Useful Commands

```bash
# from main
git checkout main
git pull origin main

# create feature branch
git checkout -b feature/failure-states

# push branch
git push -u origin feature/failure-states

# open PR in browser (after pushing)
gh pr create --base main --head feature/failure-states --fill
```