using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;

// Добавить на любой GameObject в сцене для мгновенного мониторинга
public class InstantPerformanceMonitor : MonoBehaviour
{
    [Header("Real-time Display")]
    public KeyCode toggleKey = KeyCode.F1;

    private bool showUI = true;
    private GUIStyle labelStyle;
    private PerformanceData currentData = new PerformanceData();

    private ProfilerRecorder totalAllocatedMemoryRecorder;
    private ProfilerRecorder mainThreadTimeRecorder;


    struct PerformanceData
    {
        public float fps;
        public float frameTime;
        public float memoryMB;
        public string status;
    }

    void OnEnable()
    {
        totalAllocatedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
        mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
    }

    void OnDisable()
    {
        totalAllocatedMemoryRecorder.Dispose();
        mainThreadTimeRecorder.Dispose();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showUI = !showUI;
        }

        // Обновляем данные каждые 10 кадров для плавности
        if (Time.frameCount % 10 == 0)
        {
            UpdatePerformanceData();
        }
    }

    void UpdatePerformanceData()
    {
        currentData.fps = 1.0f / Time.smoothDeltaTime;
        currentData.frameTime = mainThreadTimeRecorder.LastValue * 1e-6f; // Convert nanoseconds to milliseconds
        currentData.memoryMB = totalAllocatedMemoryRecorder.LastValue / (1024f * 1024f);

        // Определяем статус производительности
        if (currentData.fps >= 25) currentData.status = "🟢 EXCELLENT";
        else if (currentData.fps >= 20) currentData.status = "🟡 GOOD";
        else if (currentData.fps >= 15) currentData.status = "🟠 ACCEPTABLE";
        else currentData.status = "🔴 POOR";
    }

    void OnGUI()
    {
        if (!showUI) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
        }

        string performanceText = $@"
🎯 PERFORMANCE MONITOR (F1 to toggle)
═══════════════════════════════════════════
📊 FPS: {currentData.fps:F1} | Status: {currentData.status}
⏱️ Frame Time: {currentData.frameTime:F1}ms
💾 Memory: {currentData.memoryMB:F1}MB

🎮 Controls:
F1 - Toggle this display
F2 - Run quick diagnostics  
F3 - Apply GPU fix
F4 - Enable extreme optimization
═══════════════════════════════════════════";

        GUI.Label(new Rect(10, 10, 400, 300), performanceText, labelStyle);

        // Быстрые кнопки действий
        if (GUI.Button(new Rect(10, 320, 150, 30), "🚀 Apply GPU Fix"))
        {
            var fixScript = FindFirstObjectByType<CriticalPerformanceFix>();
            if (fixScript != null)
            {
                fixScript.ApplyGPUFix();
            }
        }

        if (GUI.Button(new Rect(170, 320, 150, 30), "🏃 Extreme Mode"))
        {
            var fixScript = FindFirstObjectByType<CriticalPerformanceFix>();
            if (fixScript != null)
            {
                fixScript.EnableExtremeOptimization();
            }
        }
    }
} 