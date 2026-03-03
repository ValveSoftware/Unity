using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_GLTFAST
using System.Threading.Tasks;
using GLTFast;
#endif
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;

public class RenderModelLoader : MonoBehaviour
{
    private enum Handedness { Left, Right }
    [SerializeField] private Handedness controllerHand;

    private ulong renderModelHandle;
    private List<Transform> animNodes;
    private List<ValveRenderModelFeature.ModelNodeState> animNodeStates;
    private Coroutine modelLoadCoroutine;
    private ValveRenderModelFeature feature;
    
    private void Awake()
    {
        feature = OpenXRSettings.Instance.GetFeature<ValveRenderModelFeature>();
        if (feature ==null)
        {
            Debug.LogError("RenderModelFeature not found or not enabled - disabling RenderModelLoader.");
            enabled = false;
        }
    }

    private void Update()
    {
        // Load the render model once the input device has connected.
        if (modelLoadCoroutine == null)
        {
            if (IsTrackedControllerConnected(controllerHand))
            {
                modelLoadCoroutine = StartCoroutine(LoadModelAsync());
            }
            return;
        }

        UpdateAnimations();
    }
    
    private IEnumerator LoadModelAsync()
    {
        // Load the binary data for the render model
        ulong renderModelAssetHandle;
        List<string> animNodeNames;
        byte[] assetBytes;
        if (!feature.GetRenderModelAssetData(controllerHand == Handedness.Left,
                out renderModelHandle,
                out renderModelAssetHandle,
                out animNodeNames,
                out assetBytes))
        {
            Debug.LogError("Failed to retrieve glTF data from runtime.");
            yield break;
        }

#if UNITY_GLTFAST
        // Import GLTF binary data and instantiate  hierarchy under this transform 
        var gltf = new GltfImport();
        Task<bool> task = gltf.Load(assetBytes);
        yield return new WaitUntil(() => task.IsCompleted);
        if (!task.Result)
        {
            Debug.LogError($"glTF import error (code: = {task.Result}");
            yield break;
        }
        task = gltf.InstantiateMainSceneAsync(transform);
        yield return new WaitUntil(() => task.IsCompleted);
        if (!task.Result)
        {
            Debug.LogError($"glTF scene instantiation error (code: {task.Result}).");
            yield break;
        }
        
        // Adjust for SteamVR authoring
        if (transform.childCount > 0)
        {
            var child = transform.GetChild(0);
            AdjustModelBasis(child.transform);
        }
#endif

        // Setup animation state data to use for updating render model animatable nodes
        int animStateCount = animNodeNames.Count;
        animNodeStates = Enumerable.Range(0, animStateCount)
            .Select(_ => new ValveRenderModelFeature.ModelNodeState())
            .ToList();

        // Find the animatable nodes in the game object hierarchy base on the list of object names provided by OpenXR
        // Store them in a list so their transforms can up updated when render model nodes' state changes.
        animNodes = new List<Transform>();
        foreach (var name in animNodeNames)
        {
            var child = FindChildRecursive(transform, name);
            if (child)
            {
                animNodes.Add(child);
            }
        }

        // Free up the resources used for the glhf transmission data now that the model's hierarchy has been created.
        if (renderModelAssetHandle != 0)
        {
            if (!feature.DestroyRenderModel(renderModelAssetHandle))
            {
                Debug.LogError("[RenderModelLoader] Error destroying temporary render model.");
                yield break;
            }
        }
    }

    private void UpdateAnimations()
    {
        // Get the poses from OpenXR and update the transforms of those that are visible.
        if (renderModelHandle == 0) return;
        if (animNodeStates == null) return;
    
        if (feature.GetRenderModelStates(renderModelHandle, ref animNodeStates))
        {
            for (int i = 0; i < animNodeStates.Count; ++i)
            {
                var state = animNodeStates[i];
                var node = animNodes[i];

                node.gameObject.SetActive(state.Visible);
                if (state.Visible)
                {
                    AdjustLocalAnimUpdate(ref state.Position, ref state.Rotation);
                    node.SetLocalPositionAndRotation(state.Position, state.Rotation);
                }
            }
        }
    }
    
    private static bool IsTrackedControllerConnected(Handedness handedness)
    {
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevices(inputDevices);
        InputDeviceCharacteristics desiredCharacteristics = 
            InputDeviceCharacteristics.Controller | 
            (handedness == Handedness.Left ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right);

        foreach (var inputDevice in inputDevices)
        {
            if ((inputDevice.characteristics & desiredCharacteristics) == desiredCharacteristics &&
                inputDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) &&
                isTracked)
            {
                return true;
            }
        }
        return false;
    }
    
    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void AdjustModelBasis(Transform transform)
    {
        // Adjust for facing direction of SteamVR model authoring 
        transform.localRotation *= Quaternion.Euler(0, 180, 0);
    }
    
    private static void AdjustLocalAnimUpdate(ref Vector3 position, ref Quaternion rotation)
    {
        // Compensate for RH -> LH reflection (GLTF -> Unity) and original model basis change
        position.x = -position.x;
        position.z = -position.z;
        rotation.x = -rotation.x;
        rotation.z = -rotation.z;
    }
}
