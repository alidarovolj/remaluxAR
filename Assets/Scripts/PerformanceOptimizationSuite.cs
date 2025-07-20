using UnityEngine;

// Создать GameObject "PerformanceOptimizationSuite" и добавить этот скрипт
public class PerformanceOptimizationSuite : MonoBehaviour
{
    [Header("Optimization Components")]
    public QuickDiagnostics diagnostics;
    public CriticalPerformanceFix criticalFix;
    public InstantPerformanceMonitor monitor;

    [ContextMenu("Initialize Full Suite")]
    void InitializeOptimizationSuite()
    {
        if (GetComponent<QuickDiagnostics>() == null)
            gameObject.AddComponent<QuickDiagnostics>();
        if (GetComponent<CriticalPerformanceFix>() == null)
            gameObject.AddComponent<CriticalPerformanceFix>();
        if (GetComponent<InstantPerformanceMonitor>() == null)
            gameObject.AddComponent<InstantPerformanceMonitor>();

        diagnostics = GetComponent<QuickDiagnostics>();
        criticalFix = GetComponent<CriticalPerformanceFix>();
        monitor = GetComponent<InstantPerformanceMonitor>();

        Debug.Log("🎯 Performance Optimization Suite initialized!");
        Debug.Log("Press F1 for real-time monitor, F2 for diagnostics");
    }

    void Start()
    {
        // Automatically initialize if components are not assigned
        if (diagnostics == null || criticalFix == null || monitor == null)
        {
            InitializeOptimizationSuite();
        }
        Debug.Log("🚀 remaluxAR Performance Optimization Suite ready!");
        Debug.Log("🔧 Run 'Initialize Full Suite' from context menu to begin");
    }
} 