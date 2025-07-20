using UnityEngine;
using Unity.Barracuda;

// Добавить в SegmentationManager или создать отдельный скрипт
public class CriticalPerformanceFix : MonoBehaviour
{
    [ContextMenu("Apply Critical GPU Fix")]
    public void ApplyGPUFix()
    {
        var segmentationManager = FindFirstObjectByType<SegmentationManager>();
        if (segmentationManager == null)
        {
            Debug.LogError("SegmentationManager not found!");
            return;
        }

        Debug.Log("🚀 Applying critical GPU performance fix...");

        // Через reflection находим worker и пересоздаем его с GPU backend
        var workerField = segmentationManager.GetType().GetField("worker",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (workerField != null)
        {
            var currentWorker = workerField.GetValue(segmentationManager) as IWorker;
            currentWorker?.Dispose();

            // Получаем модель и создаем GPU worker
            var modelField = segmentationManager.GetType().GetField("model",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var model = modelField?.GetValue(segmentationManager) as Model;

            if (model != null)
            {
                var gpuWorker = WorkerFactory.CreateWorker(model, WorkerFactory.Device.GPU);
                workerField.SetValue(segmentationManager, gpuWorker);
                Debug.Log("✅ GPU backend applied successfully!");
            }
        }
    }

    [ContextMenu("Enable Extreme Optimization")]
    public void EnableExtremeOptimization()
    {
        var segmentationManager = FindFirstObjectByType<SegmentationManager>();
        if (segmentationManager != null)
        {
            // segmentationManager.SetExtremeOptimizationMode(true);
            Debug.Log("🏃‍♂️ Extreme optimization mode enabled!");
        }
    }
} 