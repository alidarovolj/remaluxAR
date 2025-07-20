using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Profiling;
using Unity.Profiling;

// Добавить в сцену как новый GameObject с этим скриптом
[System.Serializable]
public class QuickDiagnostics : MonoBehaviour
{
    [Header("Quick Performance Check")]
    public bool runOnStart = true;
    public int measurementFrames = 300; // 5 seconds at 60fps

    private ProfilerRecorder totalAllocatedMemoryRecorder;

    void OnEnable()
    {
        totalAllocatedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
    }

    void OnDisable()
    {
        totalAllocatedMemoryRecorder.Dispose();
    }

    void Start()
    {
        if (runOnStart)
        {
            StartCoroutine(QuickPerformanceCheck());
        }
    }

    [ContextMenu("Run Quick Diagnostics")]
    public void RunDiagnostics()
    {
        StartCoroutine(QuickPerformanceCheck());
    }

    IEnumerator QuickPerformanceCheck()
    {
        Debug.Log("🔍 Starting Quick Performance Diagnostics...");

        float totalFrameTime = 0f;
        float minFPS = float.MaxValue;
        float maxFPS = 0f;
        int frameCount = 0;

        long startMemory = totalAllocatedMemoryRecorder.LastValue;

        for (int i = 0; i < measurementFrames; i++)
        {
            float currentFPS = 1.0f / Time.deltaTime;
            totalFrameTime += Time.deltaTime;

            minFPS = Mathf.Min(minFPS, currentFPS);
            maxFPS = Mathf.Max(maxFPS, currentFPS);
            frameCount++;

            yield return null;
        }

        long endMemory = totalAllocatedMemoryRecorder.LastValue;
        long memoryDelta = endMemory - startMemory;

        // Результаты диагностики
        float averageFPS = frameCount / totalFrameTime;
        float memoryMB = memoryDelta / (1024f * 1024f);

        string report = $@"
📊 QUICK PERFORMANCE REPORT:
═══════════════════════════════════════
📱 FPS Statistics:
   • Average FPS: {averageFPS:F1}
   • Min FPS: {minFPS:F1}  
   • Max FPS: {maxFPS:F1}
   • Frame consistency: {((maxFPS - minFPS) < 10 ? "✅ Good" : "❌ Unstable")}

💾 Memory Analysis:
   • Memory allocated during test: {memoryMB:F2} MB
   • Memory/second: {(memoryMB / (totalFrameTime)):F2} MB/s
   • GC pressure: {(memoryMB > 10 ? "🚨 HIGH" : memoryMB > 5 ? "⚠️ Medium" : "✅ Low")}

🎯 Priority Actions:
   {(averageFPS < 15 ? "🚨 CRITICAL: FPS too low - apply GPU backend fix immediately!" : "")}
   {(memoryMB > 10 ? "🚨 CRITICAL: High memory allocation - fix GC issues!" : "")}
   {(minFPS < averageFPS * 0.7f ? "⚠️ WARNING: Frame time spikes detected!" : "")}

🔧 Next Steps:
   1. {(averageFPS < 20 ? "Switch Barracuda to GPU backend" : "✅ FPS acceptable")}
   2. {(memoryMB > 5 ? "Profile and fix memory allocations" : "✅ Memory usage OK")}
   3. Run full PerformanceBenchmark for detailed analysis
═══════════════════════════════════════";

        Debug.Log(report);

        // Сохраняем отчет в файл
        string filePath = Path.Combine(Application.persistentDataPath, "quick_diagnostics.txt");
        File.WriteAllText(filePath, report);
        Debug.Log($"💾 Report saved to: {filePath}");
    }
} 