using UnityEngine;

[System.Serializable]
public class PerformanceSettings
{
    public Vector2Int imageSize;
    public float runSchedule;
    public int frameSkip;
    public int targetFPS;
    public string qualityLevel;
}

public class PerformanceManager : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField, Tooltip("Минимальный FPS для поддержания производительности")]
    private float targetMinFPS = 12f;
    
    [SerializeField, Tooltip("Время в секундах для оценки производительности")]
    private float evaluationPeriod = 5f;
    
    [SerializeField, Tooltip("Включить автоматическую адаптацию качества")]
    private bool enableAdaptiveQuality = true;

    [Header("Quality Levels")]
    [SerializeField]
    private PerformanceSettings highQuality = new PerformanceSettings
    {
        imageSize = new Vector2Int(256, 256),
        runSchedule = 0.8f,
        frameSkip = 2,
        targetFPS = 20,
        qualityLevel = "High"
    };
    
    [SerializeField]
    private PerformanceSettings mediumQuality = new PerformanceSettings
    {
        imageSize = new Vector2Int(192, 192),
        runSchedule = 1.0f,
        frameSkip = 3,
        targetFPS = 15,
        qualityLevel = "Medium"
    };
    
    [SerializeField]
    private PerformanceSettings lowQuality = new PerformanceSettings
    {
        imageSize = new Vector2Int(128, 128),
        runSchedule = 1.5f,
        frameSkip = 5,
        targetFPS = 12,
        qualityLevel = "Low"
    };

    // Performance tracking
    private float fpsSum = 0f;
    private int fpsCount = 0;
    private float lastEvaluationTime = 0f;
    private int currentQualityLevel = 1; // 0=Low, 1=Medium, 2=High
    private SegmentationManager segmentationManager;
    
    // Events
    public System.Action<PerformanceSettings> OnQualityChanged;

    private void Start()
    {
        segmentationManager = FindFirstObjectByType<SegmentationManager>();
        lastEvaluationTime = Time.time;
        
        // Start with medium quality
        ApplyQualitySettings(mediumQuality);
        
        Debug.Log($"PerformanceManager initialized. Target min FPS: {targetMinFPS}");
    }

    private void Update()
    {
        if (!enableAdaptiveQuality) return;

        // Track FPS
        TrackPerformance();
        
        // Evaluate and adjust quality if needed
        if (Time.time - lastEvaluationTime >= evaluationPeriod)
        {
            EvaluateAndAdjustQuality();
            lastEvaluationTime = Time.time;
        }
    }

    private void TrackPerformance()
    {
        float currentFPS = 1.0f / Time.unscaledDeltaTime;
        fpsSum += currentFPS;
        fpsCount++;
    }

    private void EvaluateAndAdjustQuality()
    {
        if (fpsCount == 0) return;

        float averageFPS = fpsSum / fpsCount;
        
        // Reset tracking
        fpsSum = 0f;
        fpsCount = 0;

        // Determine if we need to adjust quality
        bool shouldDecrease = averageFPS < targetMinFPS && currentQualityLevel > 0;
        bool shouldIncrease = averageFPS > (targetMinFPS + 5f) && currentQualityLevel < 2;

        if (shouldDecrease)
        {
            currentQualityLevel--;
            Debug.Log($"Performance low (FPS: {averageFPS:F1}), reducing quality to level {currentQualityLevel}");
            ApplyCurrentQuality();
        }
        else if (shouldIncrease)
        {
            currentQualityLevel++;
            Debug.Log($"Performance good (FPS: {averageFPS:F1}), increasing quality to level {currentQualityLevel}");
            ApplyCurrentQuality();
        }
    }

    private void ApplyCurrentQuality()
    {
        PerformanceSettings settings = GetCurrentQualitySettings();
        ApplyQualitySettings(settings);
    }

    private PerformanceSettings GetCurrentQualitySettings()
    {
        return currentQualityLevel switch
        {
            0 => lowQuality,
            1 => mediumQuality,
            2 => highQuality,
            _ => mediumQuality
        };
    }

    private void ApplyQualitySettings(PerformanceSettings settings)
    {
        Application.targetFrameRate = settings.targetFPS;
        
        OnQualityChanged?.Invoke(settings);
        
        Debug.Log($"Applied {settings.qualityLevel} quality: " +
                 $"{settings.imageSize.x}x{settings.imageSize.y}, " +
                 $"{settings.runSchedule}s interval, " +
                 $"skip {settings.frameSkip} frames, " +
                 $"target {settings.targetFPS} FPS");
    }

    // Public methods for manual control
    public void SetQualityLevel(int level)
    {
        currentQualityLevel = Mathf.Clamp(level, 0, 2);
        ApplyCurrentQuality();
    }

    public void SetAdaptiveQuality(bool enabled)
    {
        enableAdaptiveQuality = enabled;
        Debug.Log($"Adaptive quality {(enabled ? "enabled" : "disabled")}");
    }

    public PerformanceSettings GetCurrentSettings()
    {
        return GetCurrentQualitySettings();
    }

    public string GetCurrentQualityName()
    {
        return GetCurrentQualitySettings().qualityLevel;
    }

    // Called by external components to report performance issues
    public void ReportPerformanceIssue()
    {
        if (currentQualityLevel > 0)
        {
            currentQualityLevel--;
            Debug.Log("Performance issue reported, reducing quality");
            ApplyCurrentQuality();
        }
    }
} 