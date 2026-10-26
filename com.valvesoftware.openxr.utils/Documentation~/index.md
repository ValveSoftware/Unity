# Valve OpenXR Utilities package

The Valve OpenXR Utilities package provides support for OpenXR projects and their access to Unity's OpenXR functionality.

## What this plugin provides

* Settings for foveated rendering
* Settings for multiview render regions 
* Refresh rate feature and sample
* Render models feature and sample
* System info feature and sample
* Project validation for Lepton projects
* Interaction profile for the Steam Frame controller

## Installation

* Open the Package Manager from Menu -> Windows
* Install package from Git URL

##### Http URL
```console
https://github.com/ValveSoftware/Unity.git?path=com.valvesoftware.openxr.utils
```

##### Git URL
```console
git@github.com:ValveSoftware/Unity.git?path=com.valvesoftware.openxr.utils
```

* Locate the plugin's OpenXR feature set and features in the editor menu (Edit -> Project Settings -> XR Plug-in Management -> OpenXR)
* Enable and configure features as appropriate for your project.

## Open XR Features

| **Name** | **Description** | **Requirements**
| :--- | :--- | :--- |
| **Settings for Unity's Rendering** | OpenXR rendering settings | Unity 2022.3, Unity OpenXR Plugin v1.16.0 |
| **Settings for Unity's Foveated Rendering** | Settings for enabling foveated rendering on startup. | Unity 2022.3, Unity OpenXR Plugin v1.16.0, Vulkan, URP |
| **Settings for Unity's Render Regions** | Settings for symmetric projection and per-view viewports and render areas. | Unity 6.1, Unity OpenXR Plugin v1.16.0, Vulkan, Multi-view |
| **Lepton Validation** | Project validation rules for Lepton-enabled projects | Unity 2022.3 |
| **Refresh Rate** | Access to the OpenXR refresh rate display extension. | Unity 2022.3, Unity OpenXR Plugin v1.16.0  |
| **Display Time** | Exposes the frame's predicted display time from `xrWaitFrame` to other features. | Unity 2022.3, Unity OpenXR Plugin v1.16.0 |
| **Render Models** | Retrieves controller render models and per-frame node poses. | Unity 2022.3, Unity OpenXR Plugin v1.16.0, a runtime that supports the extensions, the Display Time feature |
| **System Info** | Reports the OpenXR system name and whether the app runs locally on Steam Frame. | Unity 2022.3, Unity OpenXR Plugin v1.16.0 |

## Steam Frame Controller Interaction Profile

Enables the `/interaction_profiles/valve/frame_controller_valve` interaction profile from the `XR_VALVE_frame_controller_interaction` extension and exposes a `<SteamFrameController>` device layout to the Unity Input System.

Without this profile, Steam Frame controllers are presented to your application as emulated Oculus Touch controllers. Enable this profile to bind to Frame-specific buttons (the four face buttons on each hand, the shoulder buttons, and the View button) and to receive Frame-specific controller poses.

#### Enabling the profile

1. Open **Edit > Project Settings > XR Plug-in Management > OpenXR**.
2. Under **Interaction Profiles**, add **Steam Frame Controller Profile**.
3. Do this on both the **Android** tab (for builds that run on the headset) and the **Standalone** tab (for Play mode in the Editor through SteamVR), so bindings behave the same in both.

The feature class is `Valve.OpenXR.Utils.SteamFrameControllerProfile` and the device layout class is `Valve.OpenXR.Utils.SteamFrameControllerProfile.SteamFrameController`. The device layout carries the `LeftHand` and `RightHand` common usages, so bindings such as `<SteamFrameController>{LeftHand}/trigger` work as they do for other OpenXR controller layouts.

#### Face buttons

Each controller has four face buttons in a diamond. The right controller labels them A, B, X, and Y. The left controller labels them as a D-pad. The layout exposes them by physical position so that one binding covers both hands, and also exposes the hand-specific names as aliases and usages.

| **Position** | **Right hand** | **Left hand** | **Control** | **Aliases** | **Usages** |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Top | Y | D-pad up | `faceButtonTop` | `buttonTop`, `buttonY`, `buttonDpadUp` | `FaceButtonTop`, `YButton`, `DpadUpButton` |
| Outside | B | D-pad left | `faceButtonOutside` | `buttonOutside`, `buttonB`, `buttonDpadLeft` | `FaceButtonOutside`, `BButton`, `DpadLeftButton` |
| Bottom | A | D-pad down | `faceButtonBottom` | `buttonBottom`, `buttonA`, `buttonDpadDown` | `PrimaryButton`, `FaceButtonBottom`, `AButton`, `DpadDownButton` |
| Inside | X | D-pad right | `faceButtonInside` | `buttonInside`, `buttonX`, `buttonDpadRight` | `SecondaryButton`, `FaceButtonInside`, `XButton`, `DpadRightButton` |

Each face button also has a capacitive touch control named with a `Touched` suffix (for example `faceButtonTopTouched`) whose usages end in `Touch` (for example `FaceButtonTopTouch`, `YButtonTouch`, `DpadUpButtonTouch`).

#### Controls

| **Control** | **Type** | **Usages** | **OpenXR path (right hand)** | **OpenXR path (left hand)** |
| :--- | :--- | :--- | :--- | :--- |
| `thumbstick` | Vector2 | `Primary2DAxis` | `/input/thumbstick` | `/input/thumbstick` |
| `thumbstickClicked` | Button | `Primary2DAxisClick` | `/input/thumbstick/click` | `/input/thumbstick/click` |
| `thumbstickTouched` | Button | `Primary2DAxisTouch` | `/input/thumbstick/touch` | `/input/thumbstick/touch` |
| `trigger` | Axis | `Trigger` | `/input/trigger/value` | `/input/trigger/value` |
| `triggerPressed` | Button | `TriggerButton` | `/input/trigger/click` | `/input/trigger/click` |
| `triggerTouched` | Button | `TriggerTouch` | `/input/trigger/touch` | `/input/trigger/touch` |
| `grip` | Axis | `Grip` | `/input/squeeze/value` | `/input/squeeze/value` |
| `gripPressed` | Button | `GripButton` | `/input/squeeze/click` | `/input/squeeze/click` |
| `gripTouched` | Button | `GripButtonTouch` | `/input/squeeze/touch` | `/input/squeeze/touch` |
| `shoulder` | Button | `ShoulderButton` | `/input/shoulder/click` | `/input/shoulder/click` |
| `shoulderTouched` | Button | `ShoulderButtonTouch` | `/input/shoulder/touch` | `/input/shoulder/touch` |
| `menu` | Button | `MenuButton`, `ViewButton` | `/input/menu/click` | `/input/view/click` |
| `menuTouched` | Button | `MenuButtonTouch`, `ViewButtonTouch` | `/input/menu/touch` | `/input/view/touch` |
| `system` | Button | `SystemButton` | `/input/system/click` | `/input/system/click` |
| `systemTouched` | Button | `SystemButtonTouch` | `/input/system/touch` | `/input/system/touch` |
| `faceButtonTop` | Button | see above | `/input/y/click` | `/input/dpad_up/click` |
| `faceButtonOutside` | Button | see above | `/input/b/click` | `/input/dpad_left/click` |
| `faceButtonBottom` | Button | see above | `/input/a/click` | `/input/dpad_down/click` |
| `faceButtonInside` | Button | see above | `/input/x/click` | `/input/dpad_right/click` |
| `faceButtonTopTouched` | Button | see above | `/input/y/touch` | `/input/dpad_up/touch` |
| `faceButtonOutsideTouched` | Button | see above | `/input/b/touch` | `/input/dpad_left/touch` |
| `faceButtonBottomTouched` | Button | see above | `/input/a/touch` | `/input/dpad_down/touch` |
| `faceButtonInsideTouched` | Button | see above | `/input/x/touch` | `/input/dpad_right/touch` |
| `devicePose` | Pose | `Device` | `/input/grip/pose` | `/input/grip/pose` |
| `pointer` | Pose | `Pointer` | `/input/aim/pose` | `/input/aim/pose` |
| `haptic` | Haptic | `Haptic` | `/output/haptic` | `/output/haptic` |

The layout also exposes `isTracked`, `trackingState`, `devicePosition`, `deviceRotation`, `pointerPosition`, and `pointerRotation` for compatibility with the XR SDK layouts, mirroring `devicePose` and `pointer`.

#### Notes

* The System button is normally reserved by the runtime to open the system dashboard. The `system` and `systemTouched` controls are exposed for completeness, but applications should not rely on receiving them.
* On the right controller, "Outside" is the button furthest from the thumbstick (B) and "Inside" is the button nearest the thumbstick (X). The left controller mirrors this with D-pad left and D-pad right.


### Render Models

The **Valve Utils: Render Models** feature (`Valve.OpenXR.Utils.ValveOpenXRRenderModelFeature`) gives access to the controller models the OpenXR runtime provides. The feature only retrieves the data. Displaying it requires a glTF importer such as Unity's glTFast package (`com.unity.cloud.gltfast`), which the Render Model sample uses.

Wait until `IsAvailable` is true (an OpenXR session exists and the runtime supports the extensions). Subscribe to `RenderModelsChanged`, which fires when the session begins and whenever a refresh of the offered models finishes. 

Call `TryGetRenderModelId(userPath, supportedGltfExtensions, out id)` with a top level user path such as `ValveOpenXRRenderModelFeature.UserPaths.LeftHand`. It returns the id of the model the runtime currently offers for that path, or false when there is none. 

To show every device the runtime has a model for, including trackers, call `GetOfferedRenderModels(supportedGltfExtensions, results)` instead. Each `OfferedRenderModel` lists its id, the user paths it is bound to and `PoseUserPath`, the one path its pose follows when it lists several.

Call `CreateRenderModelAsync(id, supportedGltfExtensions, cancellationToken)` with an id to load a model on a thread. 
Call `TryCreateRenderModel(id, supportedGltfExtensions, out renderModel)` does the same on the calling thread (also takes a user path instead of the id). 

The `RenderModel` carries the model as a glTF binary (`GlbData`), the names of the nodes the runtime animates (`AnimatableNodeNames`), the runtime id (`Id`), its user paths (`UserPaths`, `PoseUserPath`).

Import the glTF binary and find the animatable nodes by name, then call `RenderModel.ReleaseGlbData()`. With glTFast, pass an `IDeferAgent` whose budget fits a VR frame (the sample uses 2 ms).

Every frame call `TryLocateRenderModel(renderModel, out pose)` and place the model at that pose relative to the XR Origin's Camera Offset. When it returns false the model is not tracked and should be hidden.

Every frame call `GetRenderModelStates(renderModel, nodeStates)` with one `ModelNodeState` per animatable node and apply the returned local pose and visibility to the matching transforms. glTF importers apply their own handedness conversion, so see the sample for the adjustment used with glTFast.

On every `RenderModelsChanged` call `TryGetRenderModelId` again. When the id differs from `RenderModel.Id` the runtime replaced the model, so destroy the old one and load the new one. New models can only be created while `IsSessionRunning` is true.

Call `DestroyRenderModel(renderModel)` when the model is no longer needed. Models are destroyed with the OpenXR session, after which `RenderModel.IsValid` is false.

Poses and node states are located at the frame's predicted display time, which is the time Unity locates its own views and controller poses at. The **Display Time** feature provides it, so Render Models requires that feature to be enabled too. Project validation reports it when it is missing and can enable it.

### Display Time

Unity's OpenXR plugin calls `xrWaitFrame` itself and does not expose the `XrFrameState` it gets back. The **Valve Utils: Display Time** feature (`Valve.OpenXR.Utils.ValveOpenXRDisplayTimeFeature`) hooks that call through the plugin's `xrGetInstanceProcAddr` chain and keeps the result for other features, the way a native OpenXR application would keep it for its frame loop. The hook costs a managed round trip per frame on the thread the plugin waits on, about a microsecond on Mono.

* `ValveOpenXRDisplayTimeFeature.TryGetPredictedDisplayTime(out long time)` returns the predicted display time of the most recent `xrWaitFrame` as `XrTime`, or false before the first frame of a session or while the feature is disabled. Pass it to `xrLocateSpace` and any other call that takes a display time.
* `TryGetPredictedDisplayPeriod(out long period)` returns the runtime's predicted frame period in nanoseconds.
* `IsAvailable` is true once a frame has been waited on in the current session. `IsHookInstalled` is true while the hook sits in the chain.

The plugin may wait on a thread other than the main thread, so read the value once per frame and reuse it for every locate in that frame.

### System Info

The **Valve Utils: System Info** feature (`Valve.OpenXR.Utils.ValveOpenXRSystemInfoFeature`) reads the system name from `xrGetSystemProperties` and tells whether the app runs locally on the Steam Frame.

* `ValveOpenXRSystemInfoFeature.IsInitialized()` is true once the feature is enabled and the OpenXR instance and system exist. Poll it before the two queries below.
* `GetHeadsetName()` returns the OpenXR system name, for example `SteamVR/OpenXR : cv` on the Frame, or null before initialization.
* `IsLocalSteamFrame()` is true when the runtime reports the Steam Frame as its system and the graphics device is the Frame's Adreno 750. A PC running SteamVR and streaming to a Frame returns false. The answer is cached once the system name has been read.


## Samples

| **Name** | **Description** | **Requirements**
| :--- | :--- | :--- |
| **Refresh Rate** | Queries and displays the current display refresh rate. | Valve Utils Refresh Rate OpenXR Feature  |
| **System Info** | Queries whether the device is Steam Frame using the System Info feature. | Valve Utils System Info OpenXR Feature |
| **Render Model** | Displays and animates the Steam Frame controller render models obtained through the Render Models feature. | Unity 2022.3, Unity OpenXR Plugin v1.16.0, Valve Utils Render Models and Display Time OpenXR Features, Unity glTFast package |

## Limitations

* Foveated rendering on Steam Frame under Unity 2022.3 may not render correctly when MSAA is enabled.

## Support

For bugs or features requests, open up a new issue if you don't see it addressed in the existing / closed issues.
