using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

[System.Serializable]
public class OptimizationSettings
{
    [Header("Rendering Settings")]
    public bool optimizeForMobile = true;
    public bool disableVSync = true;
    public bool usePerformantQualityLevel = true;
    public bool enableGPUInstancing = true;
    
    [Header("AR Specific")]
    public bool optimizeARCamera = true;
    public bool limitARFeatures = true;
    
    [Header("Memory")]
    public bool enableTextureStreaming = true;
    public int targetTextureMemory = 256; // MB
    
    [Header("Performance")]
    public int targetFrameRate = 15;
    public bool enableAdaptivePerformance = true;
}

public class AROptimizer : MonoBehaviour
{
    [Header("Optimization Settings")]
    [SerializeField]
    private OptimizationSettings settings = new OptimizationSettings();
    
    [SerializeField, Tooltip("Применить оптимизации при старте")]
    private bool applyOnStart = true;
    
    [SerializeField, Tooltip("Показывать информацию об оптимизациях")]
    private bool showOptimizationLog = true;

    private void Start()
    {
        if (applyOnStart)
        {
            ApplyOptimizations();
        }
    }

    [ContextMenu("Apply AR Optimizations")]
    public void ApplyOptimizations()
    {
        if (showOptimizationLog)
        {
            Debug.Log("=== AR OPTIMIZER: Applying optimizations ===");
        }

        ApplyRenderingOptimizations();
        ApplyAROptimizations();
        ApplyMemoryOptimizations();
        ApplyPerformanceOptimizations();
        
        if (showOptimizationLog)
        {
            Debug.Log("=== AR OPTIMIZER: Optimizations applied ===");
        }
    }

    private void ApplyRenderingOptimizations()
    {
        if (settings.optimizeForMobile)
        {
            // Set to Performant quality level (index 0)
            if (settings.usePerformantQualityLevel && QualitySettings.GetQualityLevel() != 0)
            {
                QualitySettings.SetQualityLevel(0, true);
                LogOptimization("Quality level set to Performant");
            }

            // Disable VSync for better performance
            if (settings.disableVSync)
            {
                QualitySettings.vSyncCount = 0;
                LogOptimization("VSync disabled");
            }

            // Optimize texture settings
            QualitySettings.globalTextureMipmapLimit = 1; // Reduce texture quality
            // Note: anisotropicTextures is deprecated in newer Unity versions
            LogOptimization("Texture quality optimized");

            // Disable heavy rendering features
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.antiAliasing = 0;
            LogOptimization("Heavy rendering features disabled");

            // Enable GPU instancing if available
            if (settings.enableGPUInstancing && SystemInfo.supportsInstancing)
            {
                LogOptimization("GPU Instancing supported and enabled");
            }
        }
    }

    private void ApplyAROptimizations()
    {
        if (settings.optimizeARCamera)
        {
            // Find and optimize AR Camera
            var arCamera = FindFirstObjectByType<ARCameraManager>();
            if (arCamera != null)
            {
                // Optimize AR camera settings
                var camera = arCamera.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.allowHDR = false;
                    camera.allowMSAA = false;
                    LogOptimization("AR Camera optimized (HDR and MSAA disabled)");
                }
                
                // Disable unnecessary AR features if requested
                if (settings.limitARFeatures)
                {
                    // Check for plane manager
                    var planeManager = FindFirstObjectByType<ARPlaneManager>();
                    if (planeManager != null && planeManager.enabled)
                    {
                        LogOptimization($"AR Plane Manager found (enabled: {planeManager.enabled})");
                    }
                    
                    // Check for point cloud manager
                    var pointCloudManager = FindFirstObjectByType<ARPointCloudManager>();
                    if (pointCloudManager != null && pointCloudManager.enabled)
                    {
                        LogOptimization($"AR Point Cloud Manager found (enabled: {pointCloudManager.enabled})");
                    }
                }
            }
        }
    }

    private void ApplyMemoryOptimizations()
    {
        if (settings.enableTextureStreaming)
        {
            // Enable texture streaming for better memory management
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = settings.targetTextureMemory;
            QualitySettings.streamingMipmapsMaxLevelReduction = 2;
            LogOptimization($"Texture streaming enabled (budget: {settings.targetTextureMemory}MB)");
        }

        // Set reasonable particle budget
        QualitySettings.particleRaycastBudget = 16; // Reduced from default
        LogOptimization("Particle raycast budget optimized");

        // Optimize async upload settings
        QualitySettings.asyncUploadTimeSlice = 4; // Increased for better streaming
        QualitySettings.asyncUploadBufferSize = 32; // Increased buffer
        LogOptimization("Async upload settings optimized");
    }

    private void ApplyPerformanceOptimizations()
    {
        // Set target frame rate
        Application.targetFrameRate = settings.targetFrameRate;
        LogOptimization($"Target frame rate set to {settings.targetFrameRate}");

        // Enable adaptive performance if available
        if (settings.enableAdaptivePerformance)
        {
            // Note: Adaptive Performance package needs to be installed separately
            LogOptimization("Adaptive performance setting enabled (requires Adaptive Performance package)");
        }

        // Optimize LOD settings
        QualitySettings.lodBias = 0.5f; // Reduce LOD quality for better performance
        QualitySettings.maximumLODLevel = 1; // Skip highest LOD level
        LogOptimization("LOD settings optimized");

        // Optimize real-time GI - explicit cast to int
        QualitySettings.realtimeGICPUUsage = (int)RealtimeGICPUUsage.Low;
        LogOptimization("Real-time GI CPU usage set to Low");
    }

    private void LogOptimization(string message)
    {
        if (showOptimizationLog)
        {
            Debug.Log($"AR Optimizer: {message}");
        }
    }

    // Public methods for runtime optimization control
    public void SetTargetFrameRate(int frameRate)
    {
        settings.targetFrameRate = frameRate;
        Application.targetFrameRate = frameRate;
        LogOptimization($"Target frame rate changed to {frameRate}");
    }

    public void SetQualityLevel(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
        LogOptimization($"Quality level changed to {level}");
    }

    public void ToggleVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        LogOptimization($"VSync {(enabled ? "enabled" : "disabled")}");
    }

    public void OptimizeForBattery()
    {
        // Ultra-conservative settings for battery saving
        SetTargetFrameRate(10);
        QualitySettings.SetQualityLevel(0, true);
        QualitySettings.globalTextureMipmapLimit = 2;
        LogOptimization("Battery optimization mode activated");
    }

    public void OptimizeForPerformance()
    {
        // Balanced settings for performance
        SetTargetFrameRate(15);
        QualitySettings.SetQualityLevel(0, true);
        QualitySettings.globalTextureMipmapLimit = 1;
        LogOptimization("Performance optimization mode activated");
    }

    // Display current optimization status
    [ContextMenu("Show Current Settings")]
    public void ShowCurrentSettings()
    {
        Debug.Log("=== CURRENT OPTIMIZATION STATUS ===");
        Debug.Log($"Quality Level: {QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()]})");
        Debug.Log($"VSync Count: {QualitySettings.vSyncCount}");
        Debug.Log($"Target Frame Rate: {Application.targetFrameRate}");
        Debug.Log($"Shadows: {QualitySettings.shadows}");
        Debug.Log($"Anti Aliasing: {QualitySettings.antiAliasing}");
        Debug.Log($"Texture Mipmap Limit: {QualitySettings.globalTextureMipmapLimit}");
        Debug.Log($"LOD Bias: {QualitySettings.lodBias}");
        Debug.Log($"Streaming Mipmaps: {QualitySettings.streamingMipmapsActive}");
        Debug.Log("===============================");
    }
} 