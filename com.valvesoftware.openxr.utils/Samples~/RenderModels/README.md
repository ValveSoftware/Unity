# Render Model Sample

This sample demonstrates how to use the **Valve Utils: Render Models** OpenXR feature to retrieve the controller render models from the OpenXR runtime, display them with Unity's glTFast package and animate their buttons, triggers and sticks.

The feature itself ships with the package and only provides the model data (a glTF binary plus the names of the animatable nodes) and the per-frame node poses. This sample contains the `RenderModelLoader` MonoBehaviour that imports the data with glTFast and applies the poses, together with an example scene.

## Usage

* Import the sample from the Package Manager.
* Enable the OpenXR feature in Project Settings -> XR Plug-in Management -> OpenXR -> Valve Utils: Render Models.
* Fix any project validation warnings. They install the Unity glTFast package (`com.unity.cloud.gltfast`) and add the shaders it uses with the active render pipeline to the preloaded assets so they are not stripped from builds. Without them a build shows the models in Unity's pink error shader. Switching render pipelines brings the warning back for the other pipeline's shaders.
* Open and run the `RenderModelSample` scene, or build it and run it on the headset.
* The `RenderModelLoader` objects sit under the XR Origin's Camera Offset. With `Use Render Model Pose` on, which is the default, a loader moves itself to the pose the runtime reports for its model, relative to that offset, so the model is placed by the runtime rather than by Unity's input system. The pose is located at the frame's predicted display time, the same time Unity's controller poses use, which the Display Time feature provides. Both features must be enabled.
* Turn `Use Render Model Pose` off to leave the loader where it is, for example under a tracked controller object driven by a Tracked Pose Driver, so the model follows that object instead.
* `Hide When Not Tracked`, on by default, hides the model while the runtime reports it as not tracked, for example while a controller is asleep. Turn it off to keep showing the model at its last pose.
* Each loader shows the model for the top level user path in its `User Path` field. The scene has one for `/user/hand/left` and one for `/user/hand/right`. Any path the runtime offers a model for works, for example a tracker role path.
* A loader keeps its model while the OpenXR session is stopped, for example with the headset taken off, and reloads only when the runtime offers a different model.

## Upgrading from the previous sample

Earlier versions of this package shipped the feature inside the sample as `ValveRenderModelFeature`. If you imported that version, delete the old sample folder from `Assets/Samples/Valve OpenXR Utilities/<version>/Render Model` before importing this one, then enable the new feature in the OpenXR settings.
