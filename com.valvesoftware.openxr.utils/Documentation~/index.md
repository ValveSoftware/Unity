# Valve OpenXR Utilities package

The Valve OpenXR Utilities package provides support for OpenXR projects and their access to Unity's OpenXR functionality.

## What this plugin provides

* Settings for foveated rendering
* Settings for multiview render regions 
* Refresh rate feature and sample
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
| **Settings for Unity's Rendering** | OpenXR rendering settings | Unity 2022.3, Unity OpenXR Plugin v1.9.1 |
| **Settings for Unity's Foveated Rendering** | Settings for enabling foveated rendering on startup. | Unity 2022.3, Unity OpenXR Plugin v1.13.0, Vulkan, URP |
| **Settings for Unity's Render Regions** | Settings for symmetric projection and per-view viewports and render areas. | Unity 6.1, Unity OpenXR Plugin v1.14.1, Vulkan, Multi-view |
| **Lepton Validation** | Project validation rules for Lepton-enabled projects | Unity 2022.3 |
| **Refresh Rate** | Access to the OpenXR refresh rate display extension. | Unity 2022.3, Unity OpenXR Plugin v1.9.1  |

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

## Limitations

* Foveated rendering on Steam Frame under Unity 2022.3 may not render correctly when MSAA is enabled.

## Support

For bugs or features requests, open up a new issue if you don't see it addressed in the existing / closed issues.
