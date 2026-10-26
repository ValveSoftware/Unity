# Changelog

## [1.1.0] - 2026-10-21
### Added
- New feature **Valve Utils: Display Time** (`ValveOpenXRDisplayTimeFeature`). It hooks `xrWaitFrame` through the plugin's `xrGetInstanceProcAddr` chain and exposes the frame's predicted display time and period to other features through `TryGetPredictedDisplayTime` and `TryGetPredictedDisplayPeriod`.
- New feature **Valve Utils: Render Models** (`ValveOpenXRRenderModelFeature`). Moved out of the sample and refined. Allows access to OpenXR's XR_EXT_render_model.
- New feature **Valve Utils: System Info** (`ValveOpenXRSystemInfoFeature`). Moved out of the System Info sample and refined. Reports the OpenXR system name and whether the app runs on Steam Frame itself.
- `ValveOpenXRDelegates.TryGetInstanceProc`, replaces the three copies in the Refresh Rate, Render Models and Settings for Unity's Rendering features.

### Changed
- Requires Unity OpenXR Plugin 1.16.0 or newer. The features use the plugin's `UnityEngine.XR.OpenXR.NativeTypes` (`XrResult`, `XrPosef`, `XrStructureType` and so on) instead of private copies. `Valve.OpenXR.Utils.XrVector2f` was removed in favour of the plugin's type.

### Fixed
- `EditorUtils.UsesVulkan` checked the Android graphics APIs whatever build target it was given.
- Enabling some Valve Utils features next to another feature that hooks `xrGetInstanceProcAddr` from managed code (for example the Basis Meta body tracking feature, or anything following Unity's intercept sample) threw `InvalidCastException` during session start breaking the app. `ValveOpenXRDelegates.TryFromFunctionPointer` accepts a managed hook as well as a native pointer to fix this.
- Failures while resolving OpenXR functions are now logged and leave the feature unavailable instead of throwing out of Unity's lifecycle callbacks.

### Refresh Rate
### Changed
- `ValveOpenXRRefreshRateFeature` and `OnRefreshRateFeatureAvailableDelegate` moved from the global namespace into `Valve.OpenXR.Utils`. Scripts that use them need `using Valve.OpenXR.Utils;`. The sample has it.
### Fixed
- The Refresh Rate feature called `OnSessionDestroy` from `OnSessionEnd`.
- `OnRefreshRateFeatureAvailable` lost its subscribers when the session stopped, and a throwing subscriber stopped later features from starting.
- After the session was destroyed the feature still called the runtime with the old session handle.
- `EnumerateRefreshRates` queries the runtime once per session, and the sample no longer allocates every frame.

### Render Models
### Added
- `CreateRenderModelAsync` creates a model and copies its glTF data on a worker thread.
### Changed
- Lots of performance improvements.
- Models are addressed by OpenXR top level user path (`UserPaths.LeftHand`, `/user/hand/right`, tracker roles and so on) rather than a hand flag.
- The offered models are now enumerated and described on a worker thread.
- The feature raises `RenderModelsChanged` when a refresh of the offered models finishes, when the session begins and after it is destroyed. The sample loader reacts to it instead of polling.
- The sample creates models with `CreateRenderModelAsync` and requests glTFast limit work to 2 ms per frame.
### Fixed
- Controller orientation in the sample was previously using the grip pose, in the new feature this now properly uses `xrLocateSpace` with the predicted display time from the new `ValveOpenXRDisplayTimeFeature`.
- Now only queries the runtime while a session is active.
- Controllers turned on after session start are now properly recognized.
- The sample's project validation adds the glTFast shaders the active render pipeline uses to the preloaded assets.

### Foveated Rendering
### Changed
- `foveatedRenderingLevel`, `eyeTrackedFoveation` and `GetFoveationEyeTrackedCenter` no longer allocate on every call.
- `GetFoveationEyeTrackedCenter` resolves its OpenXR function once per instance instead of retrying and logging on every call.

### System Info
### Changed
- The sample's `SystemInfoFeature` is now the package's `ValveOpenXRSystemInfoFeature`. `IsRunnginOnSteamFrame` is `IsLocalSteamFrame`, which also requires the Frame's Adreno 750 so a PC streaming to a Frame returns false.
- The feature resolves `xrGetSystemProperties` through `ValveOpenXRDelegates.TryGetInstanceProc` and no longer requires, enables or validates the Settings for Unity's Rendering feature.
- `GetHeadsetName` and `IsLocalSteamFrame` query the runtime once per system and no longer log the system name.

### Settings for Unity's Rendering
### Removed
- The feature's now redundant OpenXR access API is gone: `GetOpenXrInstanceProc`, `HasOpenXRInstance`, `GetOpenXRInstance`, `HasSession`, `GetSession` and the `OnInstanceCreated`, `OnInstanceDestroyed`, `OnSessionCreated` and `OnSessionDestroyed` events with their delegate types. Function resolution moved to `ValveOpenXRDelegates.TryGetInstanceProc`, which is now public and takes the calling feature's own `xrGetInstanceProcAddr` and instance handle.


## [1.0.9] - 2026-09-21
### Steam Frame Controller Profile
### Changed
- Renamed the bumper button to shoulder: controls `shoulder` and `shoulderTouched`, aliases `shoulderButton` and `shoulderButtonTouched`, usages `ShoulderButton` and `ShoulderButtonTouch`, constants `shoulderClick` and `shoulderTouch`, bound to `/input/shoulder/click` and `/input/shoulder/touch`. Bindings that used the `bumper` names need updating, and the runtime must expose the shoulder paths.
- The `devicepose` and `pointer` actions are now first in the action map, so the pose block sits at offset 0 and adding controls no longer shifts it.

### Fixed
- `shoulderTouched` now declares the `ShoulderButtonTouch` usage in its `ActionConfig`, matching the control and the docs.
- Controller poses were not applied. The `isTracked`, `trackingState`, `devicePosition`, `deviceRotation`, `pointerPosition` and `pointerRotation` offsets did not match the state layout, so `trackingState` read button bytes and reported the controller as untracked. They are now 0, 4, 8, 20, 68 and 80.

## [1.0.8] - 2026-09-08
### Steam Frame Controller Profile
### Changed
- Moved `SteamFrameControllerProfile` from the `UnityEngine.XR.OpenXR.Features.Interactions` namespace to `Valve.OpenXR.Utils`. The class carries a `MovedFrom` attribute so the API updater can fix references in user scripts. The `<SteamFrameController>` layout name, control names, aliases, and usages are unchanged, so existing Input Action bindings keep working.
- Changed the feature id from `com.unity.openxr.feature.input.frame_controller` to `com.valvesoftware.openxr.utils.frame_controller`.
- `triggerPressed` now binds to `/input/trigger/click` instead of deriving a press from `/input/trigger/value`.
- OpenXR action names created by the profile are now all lower case. Names containing an upper-case `I` were previously lower-cased using the current culture, which produced a dotless `ı` under Turkish locales and made `xrCreateAction` fail with `XR_ERROR_PATH_FORMAT_INVALID`.
- Fixed the layout display name, which read "Steam Frame Controller Controller (OpenXR)".
- Corrected the XML documentation on the binding constants and layout controls.
### Fixed
- `menuTouched` on the left hand was bound to `/input/view/click` instead of `/input/view/touch`.
### Added
- `system` and `systemTouched` controls with usages `SystemButton` and `SystemButtonTouch`, bound to `/input/system/click` and `/input/system/touch`. The runtime may reserve this button.
- `triggerClick` binding constant.
- Documentation for the profile, including the per-hand face button mapping and the full control table.

## [1.0.7] - 2026-07-24
### Added
- Added project validation warnings if settings that potentially interfere with foveated rendering are enabled.

## [1.0.6] - 2026-06-30
### Fixed
- Render model sample - added explicit api for checking when model data is available, minor refactoring for readability.

## [1.0.5] - 2026-04-27
### Fixed
- Fixed settings to better support foveated rendering with VK_KHR_depth_stencil_resolve

## [1.0.4] - 2026-03-03
### Added
- OpenXR render model sample.

## [1.0.3] - 2026-02-12
### Added
- System info sample for checking device is Steam Frame
- Minor fixes/additions to documentation and OpenXR feature validation rules.

## [1.0.2] - 2026-01-14
### Fixed
- Fixed app permissions preventing gaze-based foveated rendering from being enabled.

## [1.0.1] - 2026-01-12
### Updating Frame Controller Interaction Path
### Added
- The Steam Frame Controller OpenXR interaction path has changed from "/interaction_profiles/valve/frame_controller" to "/interaction_profiles/valve/frame_controller_valve" to reflect the modern OpenXR spec.

## [1.0.0] - 2025-12-17

### Initial release of the Valve OpenXR Utilities package
### Added
- Added Valve OpenXR Utilities feature set
- Added settings for rendering, foveated rendering and render regions
- Added refresh rate feature and sample
- Added project validation for Lepton

