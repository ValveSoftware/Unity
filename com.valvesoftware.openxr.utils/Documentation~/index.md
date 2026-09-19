# Valve OpenXR Utilities package

The Valve OpenXR Utilities package provides support for OpenXR projects and their access to Unity's OpenXR functionality.

## What this plugin provides

* Settings for foveated rendering
* Settings for multiview render regions 
* Refresh rate feature and sample
* Project validation for Lepton projects
* Interaction profile for the Steam Frame controller
* App space delta pose feature

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
| **Settings for Unity's Rendering** | OpenXR rendering settings | Unity 2022.3, Unity OpenXR Plugin v1.9.1 |
| **Settings for Unity's Foveated Rendering** | Settings for enabling foveated rendering on startup. | Unity 2022.3, Unity OpenXR Plugin v1.13.0, Vulkan, URP |
| **Settings for Unity's Render Regions** | Settings for symmetric projection and per-view viewports and render areas. | Unity 6.1, Unity OpenXR Plugin v1.14.1, Vulkan, Multi-view |
| **Lepton Validation** | Project validation rules for Lepton-enabled projects | Unity 2022.3 |
| **Refresh Rate** | Access to the OpenXR refresh rate display extension. | Unity 2022.3, Unity OpenXR Plugin v1.9.1  |
| **App Space Delta Pose** | Reports player locomotion and the camera depth range to the runtime each frame for Steam Frame frame synthesis. See [App Space Delta Pose](#app-space-delta-pose). | Unity 2022.3, Unity OpenXR Plugin v1.9.1, any render pipeline and graphics API |

## Interaction Profiles

| **Name** | **Description** |
| :--- | :--- |
| **Steam Frame Controller** | Interaction profile for Steam Frame controllers. |

### Steam Frame Controller Profile

Enables the `/interaction_profiles/valve/frame_controller_valve` interaction profile from the `XR_VALVE_frame_controller_interaction` extension and exposes a `<SteamFrameController>` device layout to the Unity Input System.

Without this profile, Steam Frame controllers are presented to your application as emulated Oculus Touch controllers. Enable this profile to bind to Frame-specific buttons (the four face buttons on each hand, the bumper buttons, and the View button) and to receive Frame-specific controller poses.

#### Enabling the profile

1. Open **Edit > Project Settings > XR Plug-in Management > OpenXR**.
2. Under **Interaction Profiles**, add **Steam Frame Controller Profile**.
3. Do this on both the **Android** tab (for builds that run on the headset) and the **Windows, Mac, Linux** tab (for Play mode in the Editor through SteamVR), so bindings behave the same in both.

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
| `bumper` | Button | `BumperButton` | `/input/bumper/click` | `/input/bumper/click` |
| `bumperTouched` | Button | `BumperButtonTouch` | `/input/bumper/touch` | `/input/bumper/touch` |
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

## Samples

| **Name** | **Description** | **Requirements**
| :--- | :--- | :--- |
| **Refresh Rate** | Queries and displays the current display refresh rate. | Valve Utils Refresh Rate OpenXR Feature  |
| **System Info** | Queries whether the device is Steam Frame using OpenXR's system info. | Unity 2022.3, Unity OpenXR Plugin v1.9.1  |
| **Render Model** | Displays the Steam Frame controller render models obtained from the OpenXR runtime. | Unity 6.0, Unity OpenXR Plugin v1.9.1  |

## App Space Delta Pose

### Why this exists

Steam Frame's compositor synthesizes frames from consecutive rendered frames on its own, so a Unity app does not
have to implement `XR_FB_space_warp` or `XR_EXT_frame_synthesis`, or be on a render pipeline that supports them. The
one input the runtime cannot recover by itself is **how much the application moved the player between frames**:
smooth locomotion, smooth or snap turning, riding a vehicle. Head motion is known from tracking, but player locomotion
moves the whole world under the head, and without being told about it the compositor cannot tell the two apart. The
runtime also needs the camera's near and far clip distances to interpret depth, which Unity does not send when the
app is not submitting depth itself.

Those extensions carry this as `appSpaceDeltaPose`, `nearZ` and `farZ`. The **App Space Delta Pose** feature publishes
the same values once per frame through `xrSetAppSpaceDeltaPoseVALVE`, the single entry point of the private
`XR_VALVE_app_space_delta_pose` extension, and needs nothing from the render pipeline.

The feature does nothing on runtimes that do not advertise the extension. Leaving it enabled is safe everywhere.

### Enabling

1. Open **Edit > Project Settings > XR Plug-in Management > OpenXR**.
2. Enable **Valve Utils: App Space Delta Pose** on the **Android** tab (headset) and on the
   **Windows, Mac, Linux** tab (Play mode through SteamVR, which also implements the extension so you can verify the
   integration on a PC).
3. Tell the feature what your tracking space is (next section).

The feature class is `Valve.OpenXR.Utils.ValveOpenXRAppSpaceDeltaPoseFeature`.

### What is "the tracking space"

OpenXR gives Unity head and controller poses in a *reference space* (Unity's app space; `LOCAL` for device tracking
origin, `STAGE` for floor). Your rig has a transform that maps that space into the world: the parent of the camera
that the TrackedPoseDriver writes into, sometimes called the XR Origin, camera rig, or play space. Call it **T**.

```
camera_world = T * headPose_tracking
```

Everything your locomotion code does ends up as a change to **T**. Head tracking changes `headPose_tracking` and
must never be part of what you report. The feature publishes the change in **T**'s *world* transform from one rendered
frame to the next, so anything a parent of **T** does (a seat on a moving vehicle, an elevator, a platform the player
controller follows) is included automatically. Content does not need to do anything; registering **T** once in the
base game is the whole integration.

Register it once:

```csharp
ValveOpenXRAppSpaceDeltaPoseFeature.Instance?.SetTrackingSpace(trackingSpaceTransform, xrCamera);
```

or add the `AppSpaceDeltaPoseTrackingSpace` component to that transform. The feature samples it in
`Application.onBeforeRender`, after every `Update`/`LateUpdate`, so it sees the final pose the frame renders with.

The camera argument is the one rendering to the headset; its near and far clip planes are sent alongside the delta,
converted to tracking-space meters and swapped when the platform uses a reversed depth buffer. Pass null (or leave
the component to find a Camera under the transform) to fall back to `Camera.main`.

**Rig check:** if you move or rotate the camera's own transform from script (not the TrackedPoseDriver), or apply
per-frame offsets between **T** and the camera (eye-height or avatar-scale adjustments applied to the camera rather
than to **T**), those are also player motion and belong in the delta. In that case drive an empty transform with the
effective **T**, computed as `camera_world * inverse(headPose_tracking)` from the raw head pose you received this
frame, and register that transform instead.

### What the delta is

`appSpaceDeltaPose` follows `XrFrameSynthesisInfoEXT` / `XrCompositionLayerSpaceWarpInfoFB`: **the transform the app
applied to its tracking space since the previous frame**, i.e. the current frame's tracking space expressed in the
previous frame's tracking space (Meta's `Inv(appSpacePosePrev) * appSpacePoseCurr`). Its translation is in
tracking-space meters, and it is identity when the app did not move its space. Physical head motion is never included.

Given **T** at the previous frame (`Tp`) and the current frame (`Tc`):

```
delta = inverse(Tp) * Tc

deltaRotation = inverse(Tp.rotation) * Tc.rotation
deltaPosition = inverse(Tp.rotation) * (Tc.position - Tp.position) / Tc.scale   // per-axis divide
```

Worked example: the player walks 1 m forward (+Z in Unity) with no turning, so `deltaPosition = (0, 0, +1)` in Unity
coordinates. The feature converts to OpenXR's right-handed frame for you (`z` and the `x`/`y` quaternion components
flip sign), giving `(0, 0, -1)`, which is "forward" in OpenXR.

**Scale.** If your tracking space is scaled (play-space scaling for avatar height), a scaled world-space move is a
smaller move in tracking meters. Dividing by the current scale handles that. A *change* in scale between two frames
cannot be expressed as a rigid delta; the feature reports the translation as if the scale had not changed, which is
close enough for a one-frame event.

### Discontinuities

When the player jumps rather than moves (teleport, snapping into a station, world load, manual recenter) there is
nothing to interpolate, and interpolating across the jump would be wrong. Publishing an identity delta for that frame
turns locomotion interpolation off for it, so call `MarkDiscontinuity()` in the frame the jump happens:

```csharp
ValveOpenXRAppSpaceDeltaPoseFeature.Instance?.MarkDiscontinuity();
```

The feature then publishes identity for the current frame and resumes measuring from the new pose next frame. Unity
tracking-origin changes are handled internally the same way. Not publishing at all is also read as identity by the
runtime, so a base game that publishes nothing behaves as if it never moved its space.

### Verifying the integration

* On a runtime that lacks the extension the feature is disabled at startup and never publishes; SteamVR on PC and
  Steam Frame both implement it, so Play mode through SteamVR exercises the full path.
* SteamVR logs a warning if the display times of the published deltas do not line up with the frames it composes,
  which indicates a frame pairing problem.
* Stand still: the published delta is identity. Walk forward: its translation is small, positive `z` (OpenXR
  coordinates) and grows with speed. Turn right: its rotation is a small negative yaw.

## Limitations

* Foveated rendering on Steam Frame under Unity 2022.3 may not render correctly when MSAA is enabled.

## Support

For bugs or features requests, open up a new issue if you don't see it addressed in the existing / closed issues.
