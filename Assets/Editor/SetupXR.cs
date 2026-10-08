using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

public static class SetupXR
{
    [MenuItem("Repo City/Configure XR (OpenXR + Vive profile)")]
    public static void Configure()
    {
        var buildTargetGroup = BuildTargetGroup.Standalone;

        var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
        if (generalSettings == null)
        {
            // No top-level container asset exists yet (normally auto-created the first time the
            // "XR Plug-in Management" Project Settings page is opened in the Editor UI; in batch
            // mode that never happens, so create it by hand, mirroring what that UI does).
            var perBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            System.IO.Directory.CreateDirectory("Assets/XR/Settings");
            AssetDatabase.CreateAsset(perBuildTarget, "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perBuildTarget, true);

            perBuildTarget.CreateDefaultSettingsForBuildTarget(buildTargetGroup);
            perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(buildTargetGroup);
            AssetDatabase.SaveAssets();

            generalSettings = perBuildTarget.SettingsForBuildTarget(buildTargetGroup);
        }

        if (generalSettings == null || generalSettings.Manager == null)
        {
            Debug.LogError("SetupXR: could not obtain an XRManagerSettings instance for Standalone.");
            return;
        }

        bool assigned = XRPackageMetadataStore.AssignLoader(
            generalSettings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", buildTargetGroup);
        Debug.Log($"SetupXR: OpenXR loader assigned = {assigned}");

        var openXrSettings = OpenXRSettings.ActiveBuildTargetInstance;
        if (openXrSettings == null)
        {
            Debug.LogError("SetupXR: OpenXRSettings.ActiveBuildTargetInstance is null.");
        }
        else
        {
            var vive = openXrSettings.GetFeature<HTCViveControllerProfile>();
            if (vive != null)
            {
                vive.enabled = true;
                Debug.Log("SetupXR: HTCViveControllerProfile enabled.");
            }
            else
            {
                Debug.LogWarning("SetupXR: HTCViveControllerProfile feature not found on this OpenXRSettings instance.");
            }
            EditorUtility.SetDirty(openXrSettings);
        }

        EditorUtility.SetDirty(generalSettings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SetupXR: Configure() complete.");
    }
}
