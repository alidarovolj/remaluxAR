using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class BenchmarkResults
{
    public string testName;
    public float averageFPS;
    public float averageModelTime;
    public float memoryUsage;
    public DateTime timestamp;
    public string deviceInfo;
    public Vector2Int resolution;
    public int framesTested;
}

public class PerformanceBenchmark : MonoBehaviour
{
    [Header("Benchmark Settings")]
    [SerializeField] private int benchmarkDurationSeconds = 30;
    [SerializeField] private bool autoStartBenchmark = false;
    [SerializeField] private TextMeshProUGUI benchmarkUI;
    
    [Header("Test Configurations")]
    [SerializeField] private List<BenchmarkConfig> testConfigs = new List<BenchmarkConfig>();
    
    private SegmentationManager segmentationManager;
    private List<BenchmarkResults> allResults = new List<BenchmarkResults>();
    private Coroutine currentBenchmark;
    private bool isBenchmarking = false;
    
    // Performance tracking
    private List<float> fpsData = new List<float>();
    private List<float> modelTimeData = new List<float>();
    private float startTime;
    private int frameCount;

    [System.Serializable]
    public class BenchmarkConfig
    {
        public string configName;
        public Vector2Int resolution;
        public float runSchedule;
        public int frameSkip;
        public bool extremeOptimization;
        public float maxProcessingTime;
    }

    private void Start()
    {
        segmentationManager = FindFirstObjectByType<SegmentationManager>();
        
        if (segmentationManager == null)
        {
            Debug.LogError("SegmentationManager not found! Benchmark will not work.");
            enabled = false;
            return;
        }
        
        SetupDefaultConfigs();
        
        if (autoStartBenchmark)
        {
            StartCoroutine(DelayedBenchmarkStart());
        }
    }
    
    private void SetupDefaultConfigs()
    {
        if (testConfigs.Count == 0)
        {
            testConfigs.AddRange(new[]
            {
                new BenchmarkConfig
                {
                    configName = "Original TopFormer",
                    resolution = new Vector2Int(512, 512),
                    runSchedule = 2.0f,
                    frameSkip = 5,
                    extremeOptimization = false,
                    maxProcessingTime = 300f
                },
                new BenchmarkConfig
                {
                    configName = "Optimized TopFormer",
                    resolution = new Vector2Int(384, 384),
                    runSchedule = 1.5f,
                    frameSkip = 6,
                    extremeOptimization = true,
                    maxProcessingTime = 200f
                },
                new BenchmarkConfig
                {
                    configName = "Performance Mode",
                    resolution = new Vector2Int(256, 256),
                    runSchedule = 1.0f,
                    frameSkip = 4,
                    extremeOptimization = true,
                    maxProcessingTime = 150f
                },
                new BenchmarkConfig
                {
                    configName = "Ultra Performance",
                    resolution = new Vector2Int(192, 192),
                    runSchedule = 0.8f,
                    frameSkip = 3,
                    extremeOptimization = true,
                    maxProcessingTime = 100f
                }
            });
        }
    }
    
    private IEnumerator DelayedBenchmarkStart()
    {
        yield return new WaitForSeconds(5f); // Wait for initialization
        StartCompleteBenchmark();
    }
    
    [ContextMenu("Start Complete Benchmark")]
    public void StartCompleteBenchmark()
    {
        if (isBenchmarking)
        {
            Debug.LogWarning("Benchmark already running!");
            return;
        }
        
        StartCoroutine(RunCompleteBenchmark());
    }
    
    private IEnumerator RunCompleteBenchmark()
    {
        isBenchmarking = true;
        allResults.Clear();
        
        Debug.Log("🚀 Starting Complete Performance Benchmark");
        Debug.Log($"Testing {testConfigs.Count} configurations for {benchmarkDurationSeconds}s each");
        
        UpdateUI("Starting benchmark...");
        
        for (int i = 0; i < testConfigs.Count; i++)
        {
            var config = testConfigs[i];
            
            Debug.Log($"📊 Testing Configuration {i+1}/{testConfigs.Count}: {config.configName}");
            UpdateUI($"Testing: {config.configName}\n({i+1}/{testConfigs.Count})");
            
            // Apply configuration
            ApplyConfiguration(config);
            
            // Wait for settings to apply
            yield return new WaitForSeconds(2f);
            
            // Run benchmark for this configuration
            yield return StartCoroutine(BenchmarkSingleConfig(config));
            
            // Wait between tests
            yield return new WaitForSeconds(1f);
        }
        
        // Generate final report
        GenerateBenchmarkReport();
        isBenchmarking = false;
        
        Debug.Log("✅ Complete Benchmark Finished!");
    }
    
    private void ApplyConfiguration(BenchmarkConfig config)
    {
        Debug.Log($"Applying config: {config.configName}");
        
        // Apply settings to SegmentationManager
        segmentationManager.SetImageSize(config.resolution);
        segmentationManager.SetRunSchedule(config.runSchedule);
        segmentationManager.SetFrameSkip(config.frameSkip);
        segmentationManager.SetMaxProcessingTime(config.maxProcessingTime);
        
        if (config.extremeOptimization)
        {
            segmentationManager.ToggleExtremeOptimization();
        }
    }
    
    private IEnumerator BenchmarkSingleConfig(BenchmarkConfig config)
    {
        // Reset tracking data
        fpsData.Clear();
        modelTimeData.Clear();
        frameCount = 0;
        startTime = Time.time;
        
        Debug.Log($"Starting {benchmarkDurationSeconds}s benchmark for {config.configName}");
        
        float endTime = Time.time + benchmarkDurationSeconds;
        
        while (Time.time < endTime)
        {
            // Track FPS
            float currentFPS = 1.0f / Time.unscaledDeltaTime;
            fpsData.Add(currentFPS);
            frameCount++;
            
            UpdateUI($"Testing: {config.configName}\nFPS: {currentFPS:F1}\nFrames: {frameCount}\nTime: {(endTime - Time.time):F0}s");
            
            yield return null;
        }
        
        // Calculate results
        var results = CalculateResults(config);
        allResults.Add(results);
        
        Debug.Log($"✅ {config.configName} Results: FPS={results.averageFPS:F1}, Memory={results.memoryUsage:F0}MB");
    }
    
    private BenchmarkResults CalculateResults(BenchmarkConfig config)
    {
        var results = new BenchmarkResults
        {
            testName = config.configName,
            timestamp = DateTime.Now,
            deviceInfo = $"{SystemInfo.deviceModel} - {SystemInfo.processorType}",
            resolution = config.resolution,
            framesTested = frameCount
        };
        
        // Calculate average FPS
        if (fpsData.Count > 0)
        {
            float sum = 0f;
            foreach (float fps in fpsData)
            {
                sum += fps;
            }
            results.averageFPS = sum / fpsData.Count;
        }
        
        // Get memory usage
        results.memoryUsage = System.GC.GetTotalMemory(false) / (1024f * 1024f); // MB
        
        // Note: Model time would need to be tracked from SegmentationManager
        results.averageModelTime = 0f; // TODO: Implement model time tracking
        
        return results;
    }
    
    private void GenerateBenchmarkReport()
    {
        Debug.Log("📊 === BENCHMARK REPORT ===");
        
        string report = "Performance Benchmark Results\n";
        report += $"Device: {SystemInfo.deviceModel}\n";
        report += $"Processor: {SystemInfo.processorType}\n";
        report += $"Memory: {SystemInfo.systemMemorySize}MB\n\n";
        
        // Sort by FPS (best first)
        allResults.Sort((a, b) => b.averageFPS.CompareTo(a.averageFPS));
        
        report += "Configuration Results (sorted by FPS):\n";
        report += "----------------------------------------\n";
        
        for (int i = 0; i < allResults.Count; i++)
        {
            var result = allResults[i];
            string rank = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $"{i+1}.";
            
            report += $"{rank} {result.testName}\n";
            report += $"   FPS: {result.averageFPS:F1}\n";
            report += $"   Resolution: {result.resolution.x}x{result.resolution.y}\n";
            report += $"   Memory: {result.memoryUsage:F0}MB\n";
            report += $"   Frames Tested: {result.framesTested}\n\n";
        }
        
        // Find performance improvements
        if (allResults.Count >= 2)
        {
            var best = allResults[0];
            var worst = allResults[allResults.Count - 1];
            float improvement = ((best.averageFPS - worst.averageFPS) / worst.averageFPS) * 100f;
            
            report += $"📈 Best vs Worst Performance:\n";
            report += $"   Improvement: {improvement:F1}% FPS gain\n";
            report += $"   Best: {best.testName} ({best.averageFPS:F1} FPS)\n";
            report += $"   Worst: {worst.testName} ({worst.averageFPS:F1} FPS)\n\n";
        }
        
        report += "=== END REPORT ===";
        
        Debug.Log(report);
        UpdateUI("Benchmark Complete!\nCheck Console for full report.");
        
        // Save to file (optional)
        SaveReportToFile(report);
    }
    
    private void SaveReportToFile(string report)
    {
        try
        {
            string fileName = $"BenchmarkReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Application.persistentDataPath + "/" + fileName;
            System.IO.File.WriteAllText(filePath, report);
            Debug.Log($"📄 Report saved to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save report: {e.Message}");
        }
    }
    
    private void UpdateUI(string text)
    {
        if (benchmarkUI != null)
        {
            benchmarkUI.text = text;
        }
    }
    
    [ContextMenu("Quick FPS Test")]
    public void QuickFPSTest()
    {
        StartCoroutine(QuickTest());
    }
    
    private IEnumerator QuickTest()
    {
        Debug.Log("🏃‍♂️ Quick 10-second FPS test");
        
        List<float> quickFPS = new List<float>();
        float startTime = Time.time;
        
        while (Time.time - startTime < 10f)
        {
            quickFPS.Add(1.0f / Time.unscaledDeltaTime);
            yield return null;
        }
        
        float avgFPS = quickFPS.Count > 0 ? quickFPS.ConvertAll(x => x).Average() : 0f;
        Debug.Log($"✅ Quick test result: {avgFPS:F1} FPS average");
    }
    
    public List<BenchmarkResults> GetAllResults()
    {
        return new List<BenchmarkResults>(allResults);
    }
}

// Extension method for Average
public static class ListExtensions
{
    public static float Average(this List<float> list)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (float value in list)
        {
            sum += value;
        }
        return sum / list.Count;
    }
} 