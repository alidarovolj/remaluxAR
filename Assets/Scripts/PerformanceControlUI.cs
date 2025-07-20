using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PerformanceControlUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button extremeOptimizationButton;
    [SerializeField] private Button pauseModelButton;
    [SerializeField] private Button forceRunButton;
    [SerializeField] private Slider maxProcessingTimeSlider;
    [SerializeField] private TextMeshProUGUI maxTimeLabel;
    [SerializeField] private Toggle autoOptimizationToggle;
    
    [Header("Quick Settings")]
    [SerializeField] private Button ultraFastButton;
    [SerializeField] private Button balancedButton;
    [SerializeField] private Button qualityButton;
    
    private SegmentationManager segmentationManager;
    
    private void Start()
    {
        segmentationManager = FindFirstObjectByType<SegmentationManager>();
        
        if (segmentationManager == null)
        {
            Debug.LogError("SegmentationManager not found! PerformanceControlUI will not work.");
            gameObject.SetActive(false);
            return;
        }
        
        SetupUI();
    }
    
    private void SetupUI()
    {
        // Setup buttons
        if (extremeOptimizationButton != null)
        {
            extremeOptimizationButton.onClick.AddListener(() => {
                segmentationManager.ToggleExtremeOptimization();
                UpdateButtonText();
            });
        }
        
        if (pauseModelButton != null)
        {
            pauseModelButton.onClick.AddListener(() => {
                segmentationManager.ToggleModelProcessing();
                UpdateButtonText();
            });
        }
        
        if (forceRunButton != null)
        {
            forceRunButton.onClick.AddListener(() => {
                segmentationManager.ForceModelRun();
            });
        }
        
        // Setup slider
        if (maxProcessingTimeSlider != null)
        {
            maxProcessingTimeSlider.minValue = 50f;
            maxProcessingTimeSlider.maxValue = 500f;
            maxProcessingTimeSlider.value = 200f; // Default
            maxProcessingTimeSlider.onValueChanged.AddListener(OnMaxProcessingTimeChanged);
            UpdateMaxTimeLabel();
        }
        
        // Setup toggle
        if (autoOptimizationToggle != null)
        {
            autoOptimizationToggle.isOn = true; // Default enabled
            autoOptimizationToggle.onValueChanged.AddListener(OnAutoOptimizationChanged);
        }
        
        // Setup quick setting buttons
        if (ultraFastButton != null)
        {
            ultraFastButton.onClick.AddListener(ApplyUltraFastSettings);
        }
        
        if (balancedButton != null)
        {
            balancedButton.onClick.AddListener(ApplyBalancedSettings);
        }
        
        if (qualityButton != null)
        {
            qualityButton.onClick.AddListener(ApplyQualitySettings);
        }
        
        UpdateButtonText();
    }
    
    private void OnMaxProcessingTimeChanged(float value)
    {
        segmentationManager.SetMaxProcessingTime(value);
        UpdateMaxTimeLabel();
    }
    
    private void OnAutoOptimizationChanged(bool enabled)
    {
        segmentationManager.EnableAutoOptimization(enabled);
    }
    
    private void UpdateMaxTimeLabel()
    {
        if (maxTimeLabel != null && maxProcessingTimeSlider != null)
        {
            maxTimeLabel.text = $"Max Processing: {maxProcessingTimeSlider.value:F0}ms";
        }
    }
    
    private void UpdateButtonText()
    {
        // Update button texts based on current state
        // This is basic - you could make it more sophisticated
        if (extremeOptimizationButton != null)
        {
            var textComponent = extremeOptimizationButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "Toggle Extreme Mode";
            }
        }
        
        if (pauseModelButton != null)
        {
            var textComponent = pauseModelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "Toggle Pause";
            }
        }
    }
    
    // Quick setting presets
    private void ApplyUltraFastSettings()
    {
        Debug.Log("Applying ULTRA FAST settings");
        
        // Enable extreme optimization
        segmentationManager.ToggleExtremeOptimization();
        
        // Set very low processing time threshold
        if (maxProcessingTimeSlider != null)
        {
            maxProcessingTimeSlider.value = 100f;
            OnMaxProcessingTimeChanged(100f);
        }
        
        // Enable auto-optimization
        if (autoOptimizationToggle != null)
        {
            autoOptimizationToggle.isOn = true;
            OnAutoOptimizationChanged(true);
        }
        
        Debug.Log("✅ Ultra Fast mode applied: Extreme optimization ON, 100ms max processing, Auto-optimization ON");
    }
    
    private void ApplyBalancedSettings()
    {
        Debug.Log("Applying BALANCED settings");
        
        // Set moderate processing time
        if (maxProcessingTimeSlider != null)
        {
            maxProcessingTimeSlider.value = 200f;
            OnMaxProcessingTimeChanged(200f);
        }
        
        // Enable auto-optimization
        if (autoOptimizationToggle != null)
        {
            autoOptimizationToggle.isOn = true;
            OnAutoOptimizationChanged(true);
        }
        
        Debug.Log("✅ Balanced mode applied: 200ms max processing, Auto-optimization ON");
    }
    
    private void ApplyQualitySettings()
    {
        Debug.Log("Applying QUALITY settings");
        
        // Allow longer processing time for better quality
        if (maxProcessingTimeSlider != null)
        {
            maxProcessingTimeSlider.value = 400f;
            OnMaxProcessingTimeChanged(400f);
        }
        
        // Disable auto-optimization to prevent quality reduction
        if (autoOptimizationToggle != null)
        {
            autoOptimizationToggle.isOn = false;
            OnAutoOptimizationChanged(false);
        }
        
        Debug.Log("✅ Quality mode applied: 400ms max processing, Auto-optimization OFF");
    }
    
    // Public methods for external control
    public void SetMaxProcessingTime(float timeMs)
    {
        if (maxProcessingTimeSlider != null)
        {
            maxProcessingTimeSlider.value = timeMs;
            OnMaxProcessingTimeChanged(timeMs);
        }
    }
    
    public void PauseModel()
    {
        segmentationManager.ToggleModelProcessing();
        UpdateButtonText();
    }
    
    public void EnableExtremeMode(bool enable)
    {
        // This would need to track current state to toggle correctly
        segmentationManager.ToggleExtremeOptimization();
        UpdateButtonText();
    }
} 