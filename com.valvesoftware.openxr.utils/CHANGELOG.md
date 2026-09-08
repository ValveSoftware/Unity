# Changelog

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

