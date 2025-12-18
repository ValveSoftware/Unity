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
| **Settings for Unity's Foveated Rendering** | Settings for enabling foveated rendering on startup. | Unity 2022.3, Unity OpenXR Plugin v1.9.1, Vulkan, URP |
| **Settings for Unity's Render Regions** | Settings for symmetric projection and per-view viewports and render areas. | Unity 6.1, Unity OpenXR Plugin v1.14.1, Vulkan, Multi-view |
| **Lepton Validation** | Project validation rules for Lepton-enabled projects | Unity 2022.3 |
| **Refresh Rate** | Access to the OpenXR refresh rate display extension. | Unity 2022.3, Unity OpenXR Plugin v1.9.1  |

## Interaction Profiles

| **Name** | **Description** |
| :--- | :--- |
| **Steam Frame Controller** | Interaction profile for Steam Frame controllers. |

## Support

For bugs or features requests, open up a new issue if you don't see it addressed in the existing / closed issues.
