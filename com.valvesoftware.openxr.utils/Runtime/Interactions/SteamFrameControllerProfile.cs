using System.Collections.Generic;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;
using UnityEngine.XR.OpenXR.Input;

#if UNITY_EDITOR
using UnityEditor;
#endif
#if USE_INPUT_SYSTEM_POSE_CONTROL
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;
#else
using PoseControl = UnityEngine.XR.OpenXR.Input.PoseControl;
#endif

#if USE_STICK_CONTROL_THUMBSTICKS
using ThumbstickControl = UnityEngine.InputSystem.Controls.StickControl; // If replaced, make sure the control extends Vector2Control
#else
using ThumbstickControl = UnityEngine.InputSystem.Controls.Vector2Control;
#endif

namespace UnityEngine.XR.OpenXR.Features.Interactions
{
    /// <summary>
    /// This <see cref="OpenXRInteractionFeature"/> enables the use of Steam Frame controller interaction profiles in OpenXR.
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "Steam Frame Controller Profile",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android},
        Company = "Valve",
        Desc = "Allows for mapping input to the Steam Frame Controller interaction profile.",
        DocumentationLink = "https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md#interaction-profiles",
        OpenxrExtensionStrings = "XR_VALVE_frame_controller_interaction",
        Version = "0.0.2",
        Category = UnityEditor.XR.OpenXR.Features.FeatureCategory.Interaction,
        FeatureId = featureId)]
#endif
    public class SteamFrameControllerProfile : OpenXRInteractionFeature
    {
        /// <summary>
        /// The feature id string. This is used to give the feature a well known id for reference.
        /// </summary>
        public const string featureId = "com.unity.openxr.feature.input.frame_controller";

        /// <summary>
        /// An Input System device based on the controller interaction profile Valve frame_controller.
        /// </summary>
        [Preserve, InputControlLayout(displayName = "Steam Frame Controller Controller (OpenXR)", commonUsages = new[] { "LeftHand", "RightHand" })]
        public class SteamFrameController : XRControllerWithRumble
        {
            /// <summary>
            /// A [Vector2Control](xref:UnityEngine.InputSystem.Controls.Vector2Control)/[StickControl](xref:UnityEngine.InputSystem.Controls.StickControl) that represents the <see cref="SteamFrameControllerProfile.thumbstick"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "Primary2DAxis", "Joystick" }, usage = "Primary2DAxis")]
            public ThumbstickControl thumbstick { get; private set; }

            /// <summary>
            /// A [AxisControl](xref:UnityEngine.InputSystem.Controls.AxisControl) that represents the <see cref="SteamFrameControllerProfile.squeeze"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "GripAxis", "squeeze" }, usage = "Grip")]
            public AxisControl grip { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.squeeze"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "GripButton", "squeezeClicked" }, usage = "GripButton")]
            public ButtonControl gripPressed { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.squeeze"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "GripButtonTouched", "squeezeTouched" }, usage = "GripButtonTouch")]
            public ButtonControl gripTouched { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.system"/> <see cref="SteamFrameControllerProfile.menu"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "menuButton", "viewButton" }, usages = new[] { "MenuButton", "ViewButton" })]
            public ButtonControl menu { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.system"/> <see cref="SteamFrameControllerProfile.menu"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "menuButtonTouched", "viewButtonTouched" }, usages = new[] { "MenuButtonTouch", "ViewButtonTouch" })]
            public ButtonControl menuTouched { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.bumper"/> <see cref="SteamFrameControllerProfile.bumper"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "bumperButton" }, usage = "BumperButton")]
            public ButtonControl bumper { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.bumperTouched"/> <see cref="SteamFrameControllerProfile.bumper"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "bumperButtonTouched" }, usage = "BumperButtonTouch")]
            public ButtonControl bumperTouched { get; private set; }



            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.buttonA"/> <see cref="SteamFrameControllerProfile.buttonX"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "buttonTop", "buttonY", "buttonDpadUp" }, usages = new[] { "FaceButtonTop", "YButton", "DpadUpButton" })]
            public ButtonControl faceButtonTop { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.buttonA"/> <see cref="SteamFrameControllerProfile.buttonX"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "buttonOutside", "buttonB", "buttonDpadLeft" }, usages = new[] { "FaceButtonOutside", "BButton", "DpadLeftButton" })]
            public ButtonControl faceButtonOutside { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.buttonA"/> <see cref="SteamFrameControllerProfile.buttonX"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "buttonBottom", "buttonA", "buttonDpadDown" }, usages = new[] { "PrimaryButton", "FaceButtonBottom", "AButton", "DpadDownButton" })]
            public ButtonControl faceButtonBottom { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.buttonA"/> <see cref="SteamFrameControllerProfile.buttonX"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "buttonInside", "buttonX", "buttonDpadRight" }, usages = new[] { "SecondaryButton", "FaceButtonInside", "XButton", "DpadRightButton" })]
            public ButtonControl faceButtonInside { get; private set; }



            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.buttonA"/> <see cref="SteamFrameControllerProfile.buttonX"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "buttonTopTouched", "buttonYTouched", "buttonDpadUpTouched" }, usages = new[] { "FaceButtonTopTouch", "YButtonTouch", "DpadUpButtonTouch" })]
            public ButtonControl faceButtonTopTouched { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.buttonA"/> <see cref="SteamFrameControllerProfile.buttonX"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "buttonOutsideTouched", "buttonBTouched", "buttonDpadLeftTouched" }, usages = new[] { "FaceButtonOutsideTouch", "BButtonTouch", "DpadLeftButtonTouch" })]
            public ButtonControl faceButtonOutsideTouched { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.buttonA"/> <see cref="SteamFrameControllerProfile.buttonX"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "buttonBottomTouched", "buttonATouched", "buttonDpadDownTouched" }, usages = new[] { "PrimaryButtonTouch", "FaceButtonBottomTouch", "AButtonTouch", "DpadDownButtonTouch" })]
            public ButtonControl faceButtonBottomTouched { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.buttonA"/> <see cref="SteamFrameControllerProfile.buttonX"/> OpenXR bindings, depending on handedness.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "buttonInsideTouched", "buttonXTouched", "buttonDpadRightTouched" }, usages = new[] { "SecondaryButtonTouch", "FaceButtonInsideTouch", "XButtonTouch", "DpadRightButtonTouch" })]
            public ButtonControl faceButtonInsideTouched { get; private set; }



            /// <summary>
            /// A [AxisControl](xref:UnityEngine.InputSystem.Controls.AxisControl) that represents the <see cref="SteamFrameControllerProfile.trigger"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(usage = "Trigger")]
            public AxisControl trigger { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.trigger"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "indexButton", "indexTouched", "triggerbutton" }, usage = "TriggerButton")]
            public ButtonControl triggerPressed { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.triggerTouch"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "indexTouch", "indexNearTouched" }, usage = "TriggerTouch")]
            public ButtonControl triggerTouched { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.thumbstickClick"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "JoystickOrPadPressed", "thumbstickClick", "joystickClicked" }, usage = "Primary2DAxisClick")]
            public ButtonControl thumbstickClicked { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the <see cref="SteamFrameControllerProfile.thumbstickTouch"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(aliases = new[] { "JoystickOrPadTouched", "thumbstickTouch", "joystickTouched" }, usage = "Primary2DAxisTouch")]
            public ButtonControl thumbstickTouched { get; private set; }



            /// <summary>
            /// A <see cref="PoseControl"/> that represents the <see cref="SteamFrameControllerProfile.grip"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(offset = 0, aliases = new[] { "device", "gripPose" }, usage = "Device")]
            public PoseControl devicePose { get; private set; }

            /// <summary>
            /// A <see cref="PoseControl"/> that represents the <see cref="SteamFrameControllerProfile.aim"/> OpenXR binding.
            /// </summary>
            [Preserve, InputControl(offset = 0, alias = "aimPose", usage = "Pointer")]
            public PoseControl pointer { get; private set; }

            /// <summary>
            /// A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) required for backwards compatibility with the XRSDK layouts. This represents the overall tracking state of the device. This value is equivalent to mapping devicePose/isTracked.
            /// </summary>
            [Preserve, InputControl(offset = 28, usage = "IsTracked")]
            new public ButtonControl isTracked { get; private set; }

            /// <summary>
            /// A [IntegerControl](xref:UnityEngine.InputSystem.Controls.IntegerControl) required for backwards compatibility with the XRSDK layouts. This represents the bit flag set to indicate what data is valid. This value is equivalent to mapping devicePose/trackingState.
            /// </summary>
            [Preserve, InputControl(offset = 32, usage = "TrackingState")]
            new public IntegerControl trackingState { get; private set; }

            /// <summary>
            /// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for backwards compatibility with the XRSDK layouts. This is the device position. For the device, this is both the grip and the pointer position. This value is equivalent to mapping devicePose/position.
            /// </summary>
            [Preserve, InputControl(offset = 40, noisy = true, alias = "gripPosition")]
            new public Vector3Control devicePosition { get; private set; }

            /// <summary>
            /// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the device orientation. For the device, this is both the grip and the pointer rotation. This value is equivalent to mapping devicePose/rotation.
            /// </summary>
            [Preserve, InputControl(offset = 52, noisy = true, alias = "gripOrientation")]
            new public QuaternionControl deviceRotation { get; private set; }

            /// <summary>
            /// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for back compatibility with the XRSDK layouts. This is the pointer position. This value is equivalent to mapping pointerPose/position.
            /// </summary>
            [Preserve, InputControl(offset = 100)]
            public Vector3Control pointerPosition { get; private set; }

            /// <summary>
            /// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the pointer rotation. This value is equivalent to mapping pointerPose/rotation.
            /// </summary>
            [Preserve, InputControl(offset = 112, alias = "pointerOrientation")]
            public QuaternionControl pointerRotation { get; private set; }

            /// <summary>
            /// A <see cref="HapticControl"/> that represents the <see cref="SteamFrameControllerProfile.haptic"/> binding.
            /// </summary>
            [Preserve, InputControl(usage = "Haptic")]
            public HapticControl haptic { get; private set; }

            /// <summary>
            /// Internal call used to assign controls to the the correct element.
            /// </summary>
            protected override void FinishSetup()
            {
                base.FinishSetup();

                thumbstick = GetChildControl<StickControl>("thumbstick");
                trigger = GetChildControl<AxisControl>("trigger");
                triggerPressed = GetChildControl<ButtonControl>("triggerPressed");
                triggerTouched = GetChildControl<ButtonControl>("triggerTouched");
                grip = GetChildControl<AxisControl>("grip");
                gripPressed = GetChildControl<ButtonControl>("gripPressed");
                gripTouched = GetChildControl<ButtonControl>("gripTouched");
                bumper = GetChildControl<ButtonControl>("bumper");
                bumperTouched = GetChildControl<ButtonControl>("bumperTouched");

                faceButtonTop = GetChildControl<ButtonControl>("faceButtonTop");
                faceButtonOutside = GetChildControl<ButtonControl>("faceButtonOutside");
                faceButtonBottom = GetChildControl<ButtonControl>("faceButtonBottom");
                faceButtonInside = GetChildControl<ButtonControl>("faceButtonInside");
                faceButtonTopTouched = GetChildControl<ButtonControl>("faceButtonTopTouched");
                faceButtonOutsideTouched = GetChildControl<ButtonControl>("faceButtonOutsideTouched");
                faceButtonBottomTouched = GetChildControl<ButtonControl>("faceButtonBottomTouched");
                faceButtonInsideTouched = GetChildControl<ButtonControl>("faceButtonInsideTouched");

                menu = GetChildControl<ButtonControl>("menu");
                menuTouched = GetChildControl<ButtonControl>("menuTouched");

                thumbstickClicked = GetChildControl<ButtonControl>("thumbstickClicked");
                thumbstickTouched = GetChildControl<ButtonControl>("thumbstickTouched");

                devicePose = GetChildControl<PoseControl>("devicePose");
                pointer = GetChildControl<PoseControl>("pointer");

                isTracked = GetChildControl<ButtonControl>("isTracked");
                trackingState = GetChildControl<IntegerControl>("trackingState");
                devicePosition = GetChildControl<Vector3Control>("devicePosition");
                deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
                pointerPosition = GetChildControl<Vector3Control>("pointerPosition");
                pointerRotation = GetChildControl<QuaternionControl>("pointerRotation");

                haptic = GetChildControl<HapticControl>("haptic");
            }
        }


        /// <summary>
        /// The interaction profile string used to reference Steam Frame Controller.
        /// </summary>
        public const string profile = "/interaction_profiles/valve/frame_controller"; 

        // Available Bindings
        // Left Hand Only
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/x/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
        /// </summary>
        public const string buttonDpadUp = "/input/dpad_up/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/x/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
        /// </summary>
        public const string buttonDpadUpTouch = "/input/dpad_up/touch";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/y/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
        /// </summary>
        public const string buttonDpadRight = "/input/dpad_right/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/y/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
        /// </summary>
        public const string buttonDpadRightTouch = "/input/dpad_right/touch";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/x/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
        /// </summary>
        public const string buttonDpadDown = "/input/dpad_down/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/x/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
        /// </summary>
        public const string buttonDpadDownTouch = "/input/dpad_down/touch";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/y/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
        /// </summary>
        public const string buttonDpadLeft = "/input/dpad_left/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/y/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
        /// </summary>
        public const string buttonDpadLeftTouch = "/input/dpad_left/touch";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/menu/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
        /// </summary>
        public const string buttonView = "/input/view/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/menu/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.leftHand"/> user path.
        /// </summary>
        public const string buttonViewTouch = "/input/view/touch";

        // Right Hand Only
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/a/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string buttonA = "/input/a/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/a/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string buttonATouch = "/input/a/touch";
        /// <summary>
        /// Constant for a boolean interaction binding '..."/input/b/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string buttonB = "/input/b/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/b/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string buttonBTouch = "/input/b/touch";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/x/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string buttonX = "/input/x/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/x/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string buttonXTouch = "/input/x/touch";
        /// <summary>
        /// Constant for a boolean interaction binding '..."/input/y/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string buttonY = "/input/y/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/y/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string buttonYTouch = "/input/y/touch";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/system/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string menu = "/input/menu/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/system/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string menuTouch = "/input/menu/touch";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/system/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string view = "/input/view/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/system/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. This binding is only available for the <see cref="OpenXRInteractionFeature.UserPaths.rightHand"/> user path.
        /// </summary>
        public const string viewTouch = "/input/view/touch";

        // Both Hands
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/system/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string system = "/input/system/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/system/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs. 
        /// </summary>
        public const string systemTouch = "/input/system/touch";
        /// <summary>
        /// Constant for a float interaction binding '.../input/squeeze/value' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string squeezeValue = "/input/squeeze/value";
        /// <summary>
        /// Constant for a float interaction binding '.../input/squeeze/value' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string squeezeClick = "/input/squeeze/click";
        /// <summary>
        /// Constant for a float interaction binding '.../input/squeeze/value' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string squeezeTouch = "/input/squeeze/touch";
        /// <summary>
        /// Constant for a float interaction binding '.../input/squeeze/value' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string bumperClick = "/input/bumper/click";
        /// <summary>
        /// Constant for a float interaction binding '.../input/squeeze/value' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string bumperTouch = "/input/bumper/touch";
        /// <summary>
        /// Constant for a float interaction binding '.../input/trigger/value' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string trigger = "/input/trigger/value";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/trigger/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string triggerTouch = "/input/trigger/touch";
        /// <summary>
        /// Constant for a Vector2 interaction binding '...//input/thumbstick' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string thumbstick = "/input/thumbstick";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/thumbstick/click' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string thumbstickClick = "/input/thumbstick/click";
        /// <summary>
        /// Constant for a boolean interaction binding '.../input/thumbstick/touch' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string thumbstickTouch = "/input/thumbstick/touch";
        /// <summary>
        /// Constant for a pose interaction binding '.../input/grip/pose' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string grip = "/input/grip/pose";
        /// <summary>
        /// Constant for a pose interaction binding '.../input/aim/pose' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string aim = "/input/aim/pose";
        /// <summary>
        /// Constant for a haptic interaction binding '.../output/haptic' OpenXR Input Binding. Used by input subsystem to bind actions to physical inputs.
        /// </summary>
        public const string haptic = "/output/haptic";

        private const string kDeviceLocalizedName = "Steam Frame Controller OpenXR";

        /// <summary>
        /// Registers the <see cref="SteamFrameController"/> layout with the Input System.
        /// </summary>
        protected override void RegisterDeviceLayout()
        {
            InputSystem.InputSystem.RegisterLayout(typeof(SteamFrameController),
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(kDeviceLocalizedName));
        }

        /// <summary>
        /// Removes the <see cref="SteamFrameController"/> layout from the Input System.
        /// </summary>
        protected override void UnregisterDeviceLayout()
        {
            InputSystem.InputSystem.RemoveLayout(nameof(SteamFrameController));
        }

        /// <summary>
        /// Return device layout string that used for registering device for the Input System.
        /// </summary>
        /// <returns>Device layout string.</returns>
        protected override string GetDeviceLayoutName()
        {
            return nameof(SteamFrameController);
        }

        /// <inheritdoc/>
        protected override void RegisterActionMapsWithRuntime()
        {
            ActionMapConfig actionMap = new ActionMapConfig()
            {
                name = "steamframecontroller",
                localizedName = kDeviceLocalizedName,
                desiredInteractionProfile = profile,
                manufacturer = "Valve",
                serialNumber = "",
                deviceInfos = new List<DeviceConfig>()
                {
                    new DeviceConfig()
                    {
                        characteristics = (InputDeviceCharacteristics)(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left),
                        userPath = UserPaths.leftHand
                    },
                    new DeviceConfig()
                    {
                        characteristics = (InputDeviceCharacteristics)(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right),
                        userPath = UserPaths.rightHand
                    }
                },
                actions = new List<ActionConfig>()
                {
                    // Joystick
                    new ActionConfig()
                    {
                        name = "thumbstick",
                        localizedName = "Thumbstick",
                        type = ActionType.Axis2D,
                        usages = new List<string>()
                        {
                            "Primary2DAxis"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = thumbstick,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Grip
                    new ActionConfig()
                    {
                        name = "grip",
                        localizedName = "Grip",
                        type = ActionType.Axis1D,
                        usages = new List<string>()
                        {
                            "Grip"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = squeezeValue,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Grip Pressed
                    new ActionConfig()
                    {
                        name = "gripPressed",
                        localizedName = "Grip Pressed",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "GripButton"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = squeezeClick,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Grip Touched
                    new ActionConfig()
                    {
                        name = "gripTouched",
                        localizedName = "Grip Touched",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "GripButtonTouch"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = squeezeTouch,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Menu
                    new ActionConfig()
                    {
                        name = "menu",
                        localizedName = "Menu",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "MenuButton",
                            "ViewButton"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = menu,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.rightHand }
                            },
                            new ActionBinding()
                            {
                                interactionPath = view,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.leftHand }
                            }
                        }
                    },
                    // Menu touch
                    new ActionConfig()
                    {
                        name = "menuTouched",
                        localizedName = "Menu Touched",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "MenuButtonTouch",
                            "ViewButtonTouch"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = menuTouch,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.rightHand }
                            },
                            new ActionBinding()
                            {
                                interactionPath = view,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.leftHand }
                            }
                        }
                    },




                    //topButton Press
                    new ActionConfig()
                    {
                        name = "faceButtonTop",
                        localizedName = "Face Button Top",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "FaceButtonTop", 
                            "YButton", 
                            "DpadUpButton"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = buttonY,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.rightHand }
                            },
                            new ActionBinding()
                            {
                                interactionPath = buttonDpadUp,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.leftHand }
                            },
                        }
                    },
                    //topButton Touch
                    new ActionConfig()
                    {
                        name = "faceButtonTopTouched",
                        localizedName = "Face Button Top Touched",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "FaceButtonTopTouch",
                            "YButtonTouch",
                            "DpadUpButtonTouch"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = buttonYTouch,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.rightHand }
                            },
                            new ActionBinding()
                            {
                                interactionPath = buttonDpadUpTouch,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.leftHand }
                            },
                        }
                    },
                    
                    //outsideButton Press
                    new ActionConfig()
                    {
                        name = "faceButtonOutside",
                        localizedName = "Face Button Outside",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "FaceButtonOutside",
                            "BButton",
                            "DpadLeftButton"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = buttonB,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.rightHand }
                            },
                            new ActionBinding()
                            {
                                interactionPath = buttonDpadLeft,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.leftHand }
                            },
                        }
                    },
                    //outsideButton Touch
                    new ActionConfig()
                    {
                        name = "faceButtonOutsideTouched",
                        localizedName = "Face Button Outside Touched",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "FaceButtonOutsideTouch",
                            "BButtonTouch",
                            "DpadLeftButtonTouch"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = buttonBTouch,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.rightHand }
                            },
                            new ActionBinding()
                            {
                                interactionPath = buttonDpadLeftTouch,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.leftHand }
                            },
                        }
                    },

                    //bottomButton Press
                    new ActionConfig()
                    {
                        name = "faceButtonBottom",
                        localizedName = "Face Button Bottom",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "PrimaryButton",
                            "FaceButtonBottom",
                            "AButton",
                            "DpadDownButton"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = buttonA,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.rightHand }
                            },
                            new ActionBinding()
                            {
                                interactionPath = buttonDpadDown,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.leftHand }
                            },
                        }
                    },
                    //bottomButton Touch
                    new ActionConfig()
                    {
                        name = "faceButtonBottomTouched",
                        localizedName = "Face Button Bottom Touched",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "PrimaryButtonTouch",
                            "FaceButtonBottomTouch",
                            "AButtonTouch",
                            "DpadDownButtonTouch"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = buttonATouch,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.rightHand }
                            },
                            new ActionBinding()
                            {
                                interactionPath = buttonDpadDownTouch,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.leftHand }
                            },
                        }
                    },

                    //insideButton Press
                    new ActionConfig()
                    {
                        name = "faceButtonInside",
                        localizedName = "Face Button Inside",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "SecondaryButton",
                            "FaceButtonInside",
                            "XButton",
                            "DpadRightButton"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = buttonX,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.rightHand }
                            },
                            new ActionBinding()
                            {
                                interactionPath = buttonDpadRight,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.leftHand }
                            },
                        }
                    },
                    //insideButton Touch
                    new ActionConfig()
                    {
                        name = "faceButtonInsideTouched",
                        localizedName = "Face Button Inside Touched",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "SecondaryButtonTouch",
                            "FaceButtonInsideTouch",
                            "XButtonTouch",
                            "DpadRightButtonTouch"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = buttonXTouch,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.rightHand }
                            },
                            new ActionBinding()
                            {
                                interactionPath = buttonDpadRightTouch,
                                interactionProfileName = profile,
                                userPaths = new List<string>() { UserPaths.leftHand }
                            },
                        }
                    },


                    // Trigger
                    new ActionConfig()
                    {
                        name = "trigger",
                        localizedName = "Trigger",
                        type = ActionType.Axis1D,
                        usages = new List<string>()
                        {
                            "Trigger"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = trigger,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Trigger Pressed
                    new ActionConfig()
                    {
                        name = "triggerPressed",
                        localizedName = "Trigger Pressed",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "TriggerButton"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = trigger,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    //Trigger Touch
                    new ActionConfig()
                    {
                        name = "triggerTouched",
                        localizedName = "Trigger Touched",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "TriggerTouch"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = triggerTouch,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    //Thumbstick Clicked
                    new ActionConfig()
                    {
                        name = "thumbstickClicked",
                        localizedName = "Thumbstick Clicked",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "Primary2DAxisClick"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = thumbstickClick,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    //Thumbstick Touched
                    new ActionConfig()
                    {
                        name = "thumbstickTouched",
                        localizedName = "Thumbstick Touched",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "Primary2DAxisTouch"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = thumbstickTouch,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    //Bumper
                    new ActionConfig()
                    {
                        name = "bumperButton",
                        localizedName = "Bumper Button",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "BumperButton"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = bumperClick,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    //Bumper Touched
                    new ActionConfig()
                    {
                        name = "bumperTouched",
                        localizedName = "Bumper Touched",
                        type = ActionType.Binary,
                        usages = new List<string>()
                        {
                            "BumperTouch"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = bumperTouch,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Device Pose
                    new ActionConfig()
                    {
                        name = "devicePose",
                        localizedName = "Device Pose",
                        type = ActionType.Pose,
                        usages = new List<string>()
                        {
                            "Device"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = grip,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Pointer Pose
                    new ActionConfig()
                    {
                        name = "pointer",
                        localizedName = "Pointer Pose",
                        type = ActionType.Pose,
                        usages = new List<string>()
                        {
                            "Pointer"
                        },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = aim,
                                interactionProfileName = profile,
                            }
                        }
                    },
                    // Haptics
                    new ActionConfig()
                    {
                        name = "haptic",
                        localizedName = "Haptic Output",
                        type = ActionType.Vibrate,
                        usages = new List<string>() { "Haptic" },
                        bindings = new List<ActionBinding>()
                        {
                            new ActionBinding()
                            {
                                interactionPath = haptic,
                                interactionProfileName = profile,
                            }
                        }
                    },
                }
            }; 

            AddActionMap(actionMap);
        }
    }
}
