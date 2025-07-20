using System;
using System.Collections;
using TMPro;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Linq;

public class SegmentationManager : MonoBehaviour
{
      [Header("AR")]
      [SerializeField]
      private ARCameraManager cameraManager;

      [Header("UI")]
      [SerializeField]
      private RawImage rawImage;
      [SerializeField]
      private TextMeshProUGUI classNameText;
      [Tooltip("How long the class name stays on screen in seconds.")]
      [SerializeField]
      private float displayNameDuration = 3.0f;

      [Header("Performance Monitoring")]
      [SerializeField, Tooltip("Показывать статистику производительности на экране")]
      private bool showPerformanceStats = true;
      [SerializeField]
      private TextMeshProUGUI performanceText;
      
      [Header("Performance Control")]
      [SerializeField, Tooltip("Режим экстремальной оптимизации для слабых устройств")]
      private bool extremeOptimizationMode = false;
      [SerializeField, Tooltip("Пауза обработки модели (только камера)")]
      private bool pauseModelProcessing = false;
      [SerializeField, Tooltip("Максимальное время обработки модели в миллисекундах")]
      private float maxModelProcessingTime = 200f; // 200ms

      [Header("ML Model")]
      [Tooltip("How often to run the model in seconds. A lower number is more responsive but more performance-intensive.")]
      [SerializeField]
      private float runSchedule = 1.0f; // Increased from 0.2f

      [Tooltip("The color to use for painting segmented objects.")]
      public Color paintColor = Color.blue;

      [Tooltip("The model to use for segmentation. This can be an .onnx or .tflite file.")]
      [SerializeField]
      private NNModel modelAsset;

      [Header("GPU Post-Processing")]
      [SerializeField]
      private ComputeShader postProcessShader;
      [Tooltip("Override model's default resolution. Set to 0 to disable.")]
      [SerializeField]
      private Vector2Int overrideResolution = new Vector2Int(256, 256);

      // These are now set dynamically in Start() based on the selected model
      private Vector2Int imageSize;

      // Barracuda inference
      private IWorker worker;
      private Model model;
      private Tensor lastOutputTensor;
      
      // Texture to hold the segmentation mask
      private RenderTexture segmentationTexture;
      
      // Compute Shader for post-processing
      private int postProcessKernel;
      private ComputeBuffer colorMapBuffer;
      private ComputeBuffer tensorDataBuffer; // Buffer for tensor data

      // Reusable input texture to avoid memory allocations
      private Texture2D inputTexture;

      // Reusable buffer for pixel data to avoid GC allocations
      private Color32[] pixelBuffer;

      // The class index of the object to be painted
      private int classIndexToPaint = -1; // -1 means nothing is selected for painting, show all classes

      private Coroutine displayNameCoroutine;

      // Timer for scheduling model runs
      private float lastRunTime;

      // Frame skipping for performance - now dynamic
      private int frameCounter = 0;
      private int frameSkip = 3; // Will be set by PerformanceManager

      // For double-tap detection
      private float lastTapTime = 0f;
      private const float doubleTapThreshold = 0.5f;

      // Performance monitoring
      private float frameTime = 0f;
      private int frameCount = 0;
      private float modelProcessingTime = 0f;
      private int modelRunCount = 0;
      private float averageModelTime = 0f;
      private System.DateTime lastStatsUpdate;

      // Performance management integration
      private PerformanceManager performanceManager;
      private string currentQualityLevel = "Medium";

      // Model compatibility
      private bool modelInitialized = false;
      private Vector2Int validatedImageSize;
      
      // Advanced performance tracking
      private float recentModelTimes = 0f;
      private int recentModelCount = 0;
      private float lastModelTime = 0f;
      private bool autoOptimizationEnabled = true;
      private int consecutiveSlowFrames = 0;
      private float targetModelTime = 100f; // 100ms target

      private void Start()
      {
            // Ensure the AR Camera background is enabled and uses its default material
            var arCameraBackground = cameraManager.GetComponent<ARCameraBackground>();
            if (arCameraBackground != null)
            {
                  arCameraBackground.useCustomMaterial = false;
                  arCameraBackground.enabled = true;
            }

            // A model asset is required. If not set, disable the component.
            if (modelAsset == null)
            {
                  Debug.LogError("Model Asset is not assigned. Please assign a model in the Inspector.", this);
                  enabled = false;
                  return;
            }

            // Initialize performance manager integration
            performanceManager = FindFirstObjectByType<PerformanceManager>();
            if (performanceManager != null)
            {
                  performanceManager.OnQualityChanged += OnQualitySettingsChanged;
                  Debug.Log("PerformanceManager integration enabled");
            }

            // Load the model first
            model = ModelLoader.Load(modelAsset);

            // Log model information for debugging
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"Model loaded: {modelAsset.name}");
            foreach (var input in model.inputs)
            {
                  Debug.Log($"Model input: {input.name}, shape: {input.shape}");
            }
            foreach (var output in model.outputs)
            {
                  Debug.Log($"Model output: {output}");
            }
#endif

            // Use GPU for inference. This is the single most important optimization.
            // The backend is selected by device type.
            worker = WorkerFactory.CreateWorker(model, WorkerFactory.Device.GPU);
            Debug.Log($"Using Barracuda worker type: {worker.Summary()}");

            if (overrideResolution.x > 0 && overrideResolution.y > 0)
            {
                validatedImageSize = overrideResolution;
                imageSize = overrideResolution;
                Debug.Log($"Using OVERRIDE resolution: {imageSize}");
                OnModelReady();
            }
            else
            {
                // Auto-detect optimal image size for the model
                StartCoroutine(DetectOptimalImageSize());
            }

            QualitySettings.vSyncCount = 0;   // Disable VSync for better performance

            // Initialize performance monitoring
            lastStatsUpdate = System.DateTime.Now;

            // Show all classes by default
            classIndexToPaint = -1;
      }

      void OnModelReady()
      {
            modelInitialized = true;
            CreateInputTexture();
            InitializeGpuResources();
      }

      private void InitializeGpuResources()
      {
            if (postProcessShader == null)
            {
                  // Try to find the PostProcessShader automatically
                  postProcessShader = Resources.Load<ComputeShader>("PostProcessShader");
                  if (postProcessShader == null)
                  {
                        // Alternative path - look in Shaders folder
                        var shaderGuid = UnityEditor.AssetDatabase.FindAssets("PostProcessShader t:ComputeShader");
                        if (shaderGuid.Length > 0)
                        {
                              var path = UnityEditor.AssetDatabase.GUIDToAssetPath(shaderGuid[0]);
                              postProcessShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
                        }
                  }
            }

            if (postProcessShader == null)
            {
                  Debug.LogError("Post Process Shader is not assigned!", this);
                  return;
            }

            postProcessKernel = postProcessShader.FindKernel("CSMain");

            // Create a RenderTexture for the shader to write to. This will be displayed on the UI.
            segmentationTexture = new RenderTexture(imageSize.x, imageSize.y, 0, RenderTextureFormat.ARGB32);
            segmentationTexture.enableRandomWrite = true;
            segmentationTexture.Create();
            rawImage.texture = segmentationTexture;

            // Create a buffer to hold the class colors and send it to the GPU.
            var colors = ColorMap.GetAllColors();
            colorMapBuffer = new ComputeBuffer(colors.Length, sizeof(float) * 4);
            colorMapBuffer.SetData(colors);
            postProcessShader.SetBuffer(postProcessKernel, "ColorMap", colorMapBuffer);
            
            // Create a buffer to hold tensor data (will be resized dynamically)
            int initialTensorSize = imageSize.x * imageSize.y * colors.Length;
            tensorDataBuffer = new ComputeBuffer(initialTensorSize, sizeof(float));
            postProcessShader.SetBuffer(postProcessKernel, "TensorData", tensorDataBuffer);
            
            postProcessShader.SetInt("numClasses", colors.Length);
            postProcessShader.SetTexture(postProcessKernel, "OutputTexture", segmentationTexture);
            Debug.Log("GPU resources initialized for post-processing.");
      }

      private IEnumerator DetectOptimalImageSize()
      {
            Debug.Log("=== AUTO-DETECTING OPTIMAL IMAGE SIZE ===");
            
            // Common resolutions to try, ordered by preference (performance vs quality)
            Vector2Int[] resolutionsToTry;
            
            if (modelAsset.name.Contains("TopFormer"))
            {
                  Debug.Log("TopFormer model detected. Testing compatible resolutions...");
                  // TopFormer typically needs 512x512, but we'll try optimized versions first
                  resolutionsToTry = new Vector2Int[]
                  {
                        new Vector2Int(512, 512),  // Original size - should work
                        new Vector2Int(384, 384),  // 75% of original
                        new Vector2Int(256, 256),  // 50% of original (our optimization attempt)
                        new Vector2Int(320, 320),  // Between 256 and 384
                        new Vector2Int(448, 448),  // Between 384 and 512
                  };
            }
            else
            {
                  Debug.Log("Lightweight model detected. Testing efficient resolutions...");
                  resolutionsToTry = new Vector2Int[]
                  {
                        new Vector2Int(224, 224),  // Common for lightweight models
                        new Vector2Int(256, 256),  // Slightly higher
                        new Vector2Int(192, 192),  // Lower for performance
                        new Vector2Int(160, 160),  // Very low for old devices
                        new Vector2Int(320, 320),  // Higher quality option
                  };
            }

            bool foundValidSize = false;

            foreach (var testSize in resolutionsToTry)
            {
                  Debug.Log($"Testing resolution: {testSize.x}x{testSize.y}");
                  
                  if (TestImageSize(testSize))
                  {
                        validatedImageSize = testSize;
                        imageSize = testSize;
                        foundValidSize = true;
                        
                        Debug.Log($"✅ SUCCESS: Model works with {testSize.x}x{testSize.y}");
                        break;
                  }
                  else
                  {
                        Debug.Log($"❌ FAILED: Model doesn't work with {testSize.x}x{testSize.y}");
                  }
                  
                  yield return null; // Allow other processes to run
            }

            if (!foundValidSize)
            {
                  Debug.LogError("❌ Could not find a compatible image size for this model!");
                  enabled = false;
                  yield break;
            }

            // Apply performance settings based on detected resolution
            ApplyResolutionBasedSettings();

            // Create input texture with validated size
            CreateInputTexture();
            
            // If we found a valid size, finalize setup
            if (modelInitialized)
            {
                  OnModelReady();
            }
            else
            {
                  Debug.LogError("Model initialization failed after optimal size detection.");
                  enabled = false;
                  yield break;
            }

            Debug.Log($"=== MODEL READY: Using {validatedImageSize.x}x{validatedImageSize.y} ===");
      }

      private bool TestImageSize(Vector2Int testSize)
      {
            try
            {
                  // Create a test texture
                  var testTexture = new Texture2D(testSize.x, testSize.y, TextureFormat.RGB24, false);
                  
                  // Fill with test data
                  var pixels = new Color32[testSize.x * testSize.y];
                  for (int i = 0; i < pixels.Length; i++)
                  {
                        pixels[i] = new Color32(128, 128, 128, 255); // Gray test image
                  }
                  testTexture.SetPixels32(pixels);
                  testTexture.Apply();

                  // Test model execution
                  using (var testTensor = new Tensor(testTexture, 3))
                  {
                        worker.Execute(testTensor);
                        using (var output = worker.PeekOutput())
                        {
                              // If we get here without exception, the size works
                              Debug.Log($"Model output shape: {output.width}x{output.height}x{output.channels}");
                        }
                  }

                  Destroy(testTexture);
                  return true;
            }
            catch (Exception e)
            {
                  Debug.Log($"Test failed: {e.Message}");
                  return false;
            }
      }

      private void ApplyResolutionBasedSettings()
      {
            // Auto-enable extreme mode for very high resolution models on first run
            if (validatedImageSize.x >= 512 && !extremeOptimizationMode)
            {
                  Debug.LogWarning("🚨 High resolution model detected (512x512+). Auto-enabling EXTREME optimization mode for better performance!");
                  extremeOptimizationMode = true;
            }
            
            // Adjust performance settings based on the required resolution
            if (validatedImageSize.x >= 512)
            {
                  // High resolution - very conservative settings for TopFormer
                  runSchedule = extremeOptimizationMode ? 3.0f : 2.0f; // Much slower for heavy models
                  frameSkip = extremeOptimizationMode ? 8 : 5; // Skip many more frames
                  Application.targetFrameRate = extremeOptimizationMode ? 10 : 12;
                  currentQualityLevel = extremeOptimizationMode ? "Ultra Conservative" : "Conservative";
                  targetModelTime = 300f; // Allow longer processing time for high-res models
                  Debug.Log($"Applied {currentQualityLevel.ToUpper()} settings for high resolution model (512x512+)");
                  Debug.LogWarning("TopFormer 512x512 is very demanding! Consider using extreme optimization mode.");
            }
            else if (validatedImageSize.x >= 320)
            {
                  // Medium resolution - balanced settings  
                  runSchedule = extremeOptimizationMode ? 2.0f : 1.5f;
                  frameSkip = extremeOptimizationMode ? 6 : 4;
                  Application.targetFrameRate = extremeOptimizationMode ? 12 : 15;
                  currentQualityLevel = extremeOptimizationMode ? "Conservative" : "Balanced";
                  targetModelTime = 150f;
                  Debug.Log($"Applied {currentQualityLevel} settings for medium resolution model");
            }
            else
            {
                  // Low resolution - performance settings
                  runSchedule = extremeOptimizationMode ? 1.5f : 1.0f;
                  frameSkip = extremeOptimizationMode ? 4 : 3;
                  Application.targetFrameRate = extremeOptimizationMode ? 15 : 20;
                  currentQualityLevel = extremeOptimizationMode ? "Balanced" : "Performance";
                  targetModelTime = 100f;
                  Debug.Log($"Applied {currentQualityLevel} settings for low resolution model");
            }

            // Override PerformanceManager if present
            if (performanceManager != null)
            {
                  Debug.Log("PerformanceManager will override these settings dynamically");
            }
            
            Debug.Log($"Model processing target: {targetModelTime}ms, Current schedule: {runSchedule}s, Frame skip: {frameSkip}");
            
            if (extremeOptimizationMode)
            {
                  Debug.Log("🔥 EXTREME OPTIMIZATION MODE ACTIVE 🔥");
            }
      }

      private void OnDestroy()
      {
            // Unsubscribe from performance manager events
            if (performanceManager != null)
            {
                  performanceManager.OnQualityChanged -= OnQualitySettingsChanged;
            }

            worker?.Dispose();
            lastOutputTensor?.Dispose();
            if (segmentationTexture != null) segmentationTexture.Release();
            if (colorMapBuffer != null) colorMapBuffer.Release();
            if (tensorDataBuffer != null) tensorDataBuffer.Release();
            if (inputTexture != null) Destroy(inputTexture);
      }

      private void OnQualitySettingsChanged(PerformanceSettings settings)
      {
            // Only allow resolution changes if the new size is compatible
            if (modelInitialized && settings.imageSize != validatedImageSize)
            {
                  Debug.LogWarning($"PerformanceManager tried to change resolution to {settings.imageSize.x}x{settings.imageSize.y}, " +
                                  $"but model requires {validatedImageSize.x}x{validatedImageSize.y}. Ignoring resolution change.");
                  
                  // Apply other settings but keep our validated resolution
                  runSchedule = settings.runSchedule;
                  frameSkip = settings.frameSkip;
                  currentQualityLevel = settings.qualityLevel + " (Fixed Resolution)";
            }
            else if (!modelInitialized)
            {
                  // Update settings before model initialization
                  imageSize = settings.imageSize;
                  runSchedule = settings.runSchedule;
                  frameSkip = settings.frameSkip;
                  currentQualityLevel = settings.qualityLevel;
            }

            Debug.Log($"Settings updated: {currentQualityLevel}, " +
                     $"{runSchedule}s interval, skip {frameSkip} frames");
      }

      private void CreateInputTexture()
      {
            if (inputTexture != null)
            {
                  Destroy(inputTexture);
            }
            inputTexture = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGB24, false);
      }

      private void OnEnable()
      {
            cameraManager.frameReceived += OnCameraFrameReceived;
      }

      private void OnDisable()
      {
            cameraManager.frameReceived -= OnCameraFrameReceived;
      }

      private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
      {
            // Don't process until model is properly initialized
            if (!modelInitialized) return;
            
            // Check if model processing is paused
            if (pauseModelProcessing)
            {
                  return;
            }

            // Skip frames for better performance
            frameCounter++;
            if (frameCounter <= frameSkip)
            {
                  return;
            }
            frameCounter = 0;

            // Timing-based throttling
            if (Time.time - lastRunTime < runSchedule)
            {
                  return;
            }
            
            // Auto-optimization: skip if recent model processing was too slow
            if (autoOptimizationEnabled && lastModelTime > maxModelProcessingTime)
            {
                  consecutiveSlowFrames++;
                  if (consecutiveSlowFrames > 3)
                  {
                        // Temporarily increase run schedule to give device a break
                        float tempSchedule = runSchedule * 1.5f;
                        Debug.LogWarning($"Model too slow ({lastModelTime:F0}ms), temporarily increasing schedule to {tempSchedule:F1}s");
                        
                        if (Time.time - lastRunTime < tempSchedule)
                        {
                              return;
                        }
                  }
            }
            
            lastRunTime = Time.time;
            ProcessImage();
      }

      private void ProcessImage()
      {
            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
            {
                  return;
            }

            var conversionParams = new XRCpuImage.ConversionParams
            {
                  inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                  outputDimensions = imageSize,
                  outputFormat = TextureFormat.RGB24,
                  transformation = XRCpuImage.Transformation.MirrorY
            };

            // Reuse the pre-created texture instead of creating new one
            var buffer = inputTexture.GetRawTextureData<byte>();

            try
            {
                  cpuImage.Convert(conversionParams, buffer);
            }
            finally
            {
                  cpuImage.Dispose();
            }

            inputTexture.Apply();
            StartCoroutine(RunModel(inputTexture));
      }

      private IEnumerator RunModel(Texture2D texture)
      {
            var startTime = Time.realtimeSinceStartup;
            
            using var inputTensor = new Tensor(texture, 3);

            // Schedule model execution
            worker.StartManualSchedule(inputTensor);

            // Wait for completion by checking the progress
            yield return new WaitUntil(() => worker.scheduleProgress >= 1.0f);

            // Now that we are past the yield, we can use try-catch for robust error handling
            try
            {
                  // A negative progress indicates an error during execution
                  if (worker.scheduleProgress < 0)
                  {
                        throw new System.Exception($"Model execution failed. Progress: {worker.scheduleProgress}");
                  }

                  // Get the output tensor.
                  lastOutputTensor?.Dispose();
                  lastOutputTensor = worker.PeekOutput();

                  // --- GPU Post-Processing ---
                  // Set shader parameters and dispatch.
                  
                  // Transfer tensor data to GPU buffer
                  var tensorData = lastOutputTensor.ToReadOnlyArray();
                  if (tensorDataBuffer.count != tensorData.Length)
                  {
                        tensorDataBuffer.Dispose();
                        tensorDataBuffer = new ComputeBuffer(tensorData.Length, sizeof(float));
                        postProcessShader.SetBuffer(postProcessKernel, "TensorData", tensorDataBuffer);
                  }
                  tensorDataBuffer.SetData(tensorData);
                  
                  // Set tensor dimensions for the shader
                  postProcessShader.SetInt("tensorWidth", lastOutputTensor.width);
                  postProcessShader.SetInt("tensorHeight", lastOutputTensor.height);
                  postProcessShader.SetInt("classIndexToPaint", classIndexToPaint);
                  
                  // Calculate the number of thread groups needed to cover the texture.
                  int threadGroupsX = Mathf.CeilToInt(segmentationTexture.width / 8.0f);
                  int threadGroupsY = Mathf.CeilToInt(segmentationTexture.height / 8.0f);
                  postProcessShader.Dispatch(postProcessKernel, threadGroupsX, threadGroupsY, 1);
                  // --- End of GPU Post-Processing ---

                  // Track model processing time
                  var processingTime = Time.realtimeSinceStartup - startTime;
                  lastModelTime = processingTime * 1000f; // Convert to milliseconds
                  
                  modelProcessingTime += processingTime;
                  modelRunCount++;
                  
                  // Track recent performance for auto-optimization
                  recentModelTimes += lastModelTime;
                  recentModelCount++;
                  
                  // Reset consecutive slow frames if performance is good
                  if (lastModelTime <= targetModelTime)
                  {
                        consecutiveSlowFrames = 0;
                  }

                  // Report performance issues if model takes too long
                  if (performanceManager != null && lastModelTime > maxModelProcessingTime)
                  {
                        performanceManager.ReportPerformanceIssue();
                  }
                  
                  // Log extremely slow processing
                  if (lastModelTime > maxModelProcessingTime * 2)
                  {
                        Debug.LogWarning($"⚠️ Very slow model processing: {lastModelTime:F0}ms (target: {targetModelTime:F0}ms)");
                  }
            }
            catch (System.Exception e)
            {
                  Debug.LogError($"Model execution failed: {e.Message}");
                  if (inputTensor != null)
                  {
                        Debug.LogError($"Input tensor shape: {inputTensor.shape}");
                  }
                  Debug.LogError("Model execution error occurred during runtime");
                  
                  // Report performance issue on model execution failure
                  if (performanceManager != null)
                  {
                        performanceManager.ReportPerformanceIssue();
                  }
                  
                  // Auto-pause on repeated failures
                  consecutiveSlowFrames += 5; // Treat errors as very slow frames
            }
      }

      private void LogClassStatistics()
      {
            if (lastOutputTensor == null) return;

            var classCount = new int[lastOutputTensor.channels];

            // Count pixels for each class
            for (int y = 0; y < lastOutputTensor.height; y++)
            {
                  for (int x = 0; x < lastOutputTensor.width; x++)
                  {
                        int maxClass = 0;
                        float maxScore = float.MinValue;

                        for (int c = 0; c < lastOutputTensor.channels; c++)
                        {
                              var score = lastOutputTensor[0, y, x, c];
                              if (score > maxScore)
                              {
                                    maxScore = score;
                                    maxClass = c;
                              }
                        }
                        classCount[maxClass]++;
                  }
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("=== CLASS STATISTICS ===");
            for (int i = 0; i < classCount.Length; i++)
            {
                  if (classCount[i] > 0)
                  {
                        string className = i < ColorMap.classNames.Length ? ColorMap.classNames[i] : $"Class {i}";
                        Debug.Log($"Class {i} ({className}): {classCount[i]} pixels");
                  }
            }
            Debug.Log("========================");
#endif
      }

      void Update()
      {
            // Performance monitoring
            UpdatePerformanceStats();

            // Check for screen tap
            if (Input.GetMouseButtonDown(0))
            {
                  float currentTime = Time.time;

                  // Check for double tap to clear selection
                  if (currentTime - lastTapTime < doubleTapThreshold)
                  {
                        // Double tap detected - clear selection
                        classIndexToPaint = -1;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        Debug.Log("Selection cleared. Showing all classes.");
#endif

                        if (displayNameCoroutine != null)
                        {
                              StopCoroutine(displayNameCoroutine);
                        }
                        if (classNameText != null)
                        {
                              classNameText.text = "Selection cleared";
                              classNameText.enabled = true;
                              StartCoroutine(HideTextAfterDelay());
                        }
                  }
                  else
                  {
                        // Single tap - select class
                        HandleTap(Input.mousePosition);
                  }

                  lastTapTime = currentTime;
            }
      }

      private void UpdatePerformanceStats()
      {
            if (!showPerformanceStats || performanceText == null) return;

            frameTime += Time.unscaledDeltaTime;
            frameCount++;

            // Update stats every second
            if (frameTime >= 1.0f)
            {
                  float fps = frameCount / frameTime;
                  
                  // Calculate average model processing time
                  if (modelRunCount > 0)
                  {
                        averageModelTime = (modelProcessingTime / modelRunCount) * 1000f; // Convert to milliseconds
                  }
                  
                  // Calculate recent average for more responsive monitoring
                  float recentAverage = recentModelCount > 0 ? recentModelTimes / recentModelCount : 0f;

                  // Get memory usage
                  long memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024); // MB

                  // Determine status and warnings
                  string statusText = modelInitialized ? "Ready" : "Initializing...";
                  string performanceStatus = "";
                  
                  if (pauseModelProcessing)
                  {
                        statusText = "PAUSED";
                        performanceStatus = "⏸️";
                  }
                  else if (recentAverage > maxModelProcessingTime)
                  {
                        performanceStatus = "🐌 SLOW";
                  }
                  else if (recentAverage > targetModelTime)
                  {
                        performanceStatus = "⚠️ HIGH";
                  }
                  else if (modelInitialized)
                  {
                        performanceStatus = "✅ GOOD";
                  }

                  // Update performance text with detailed info
                  performanceText.text = $"FPS: {fps:F1} {performanceStatus}\n" +
                                        $"Model: {averageModelTime:F0}ms (recent: {recentAverage:F0}ms)\n" +
                                        $"Target: {targetModelTime:F0}ms | Max: {maxModelProcessingTime:F0}ms\n" +
                                        $"Memory: {memoryUsage}MB\n" +
                                        $"Quality: {currentQualityLevel}\n" +
                                        $"Resolution: {imageSize.x}x{imageSize.y}\n" +
                                        $"Schedule: {runSchedule:F1}s | Skip: {frameSkip}\n" +
                                        $"Status: {statusText}" +
                                        (extremeOptimizationMode ? " [EXTREME]" : "") +
                                        (consecutiveSlowFrames > 0 ? $" | Slow: {consecutiveSlowFrames}" : "");

                  // Reset counters
                  frameTime = 0f;
                  frameCount = 0;
                  
                  // Reset recent tracking periodically
                  if (recentModelCount > 5)
                  {
                        recentModelTimes = 0f;
                        recentModelCount = 0;
                  }
                  
                  // Reset model timing if we have enough samples
                  if (modelRunCount > 10)
                  {
                        modelProcessingTime = 0f;
                        modelRunCount = 0;
                  }
            }
      }

      private IEnumerator HideTextAfterDelay()
      {
            yield return new WaitForSeconds(1.5f);
            if (classNameText != null)
            {
                  classNameText.enabled = false;
            }
      }

      private void HandleTap(Vector2 tapPosition)
      {
            if (lastOutputTensor == null || rawImage.texture == null)
            {
                  return;
            }

            // Convert screen point to RectTransform local point
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                  rawImage.rectTransform,
                  tapPosition,
                  null, // Camera is null for Screen Space - Overlay Canvas
                  out Vector2 localPoint))
            {
                  // Normalize the local point to be from 0 to 1
                  float normalizedX = (localPoint.x - rawImage.rectTransform.rect.x) / rawImage.rectTransform.rect.width;
                  float normalizedY = (localPoint.y - rawImage.rectTransform.rect.y) / rawImage.rectTransform.rect.height;

                  // Flip Y coordinate as texture coordinates start from bottom-left
                  normalizedY = 1.0f - normalizedY;

                  // Convert normalized coordinates to texture coordinates and clamp them to be safe
                  int texX = Mathf.Clamp((int)(normalizedX * lastOutputTensor.width), 0, lastOutputTensor.width - 1);
                  int texY = Mathf.Clamp((int)(normalizedY * lastOutputTensor.height), 0, lastOutputTensor.height - 1);

                  // Find the class index for the tapped pixel
                  int classIndex = 0;
                  float maxScore = float.MinValue;
                  for (int c = 0; c < lastOutputTensor.channels; c++)
                  {
                        var score = lastOutputTensor[0, texY, texX, c];
                        if (score > maxScore)
                        {
                              maxScore = score;
                              classIndex = c;
                        }
                  }

                  // Safety check before accessing the array
                  if (classIndex >= 0 && classIndex < ColorMap.classNames.Length)
                  {
                        // Get the class name and log it
                        string className = ColorMap.classNames[classIndex];
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        Debug.Log($"You tapped on: {className} (class index: {classIndex}). Painting it.");
#endif

                        // Show class name on UI
                        if (displayNameCoroutine != null)
                        {
                              StopCoroutine(displayNameCoroutine);
                        }
                        displayNameCoroutine = StartCoroutine(ShowClassName(className));

                        // Set the class index to be painted
                        classIndexToPaint = classIndex;
                  }
                  else
                  {
                        Debug.LogWarning($"Tapped on an object with an invalid class index: {classIndex}.");
                  }
            }
      }

      private IEnumerator ShowClassName(string name)
      {
            if (classNameText != null)
            {
                  classNameText.text = $"Tapped on: {name}";
                  classNameText.enabled = true;
                  yield return new WaitForSeconds(displayNameDuration);
                  classNameText.enabled = false;
            }
      }

      // Public methods for external control
      public void SetRunSchedule(float schedule)
      {
            runSchedule = Mathf.Max(0.1f, schedule);
      }

      public void SetFrameSkip(int skip)
      {
            frameSkip = Mathf.Max(0, skip);
      }

      public void SetImageSize(Vector2Int size)
      {
            if (modelInitialized && size != validatedImageSize)
            {
                  Debug.LogWarning($"Cannot change image size to {size.x}x{size.y}. Model requires {validatedImageSize.x}x{validatedImageSize.y}");
                  return;
            }
            
            imageSize = size;
            CreateInputTexture();
      }

      // Public methods for runtime performance control
      [ContextMenu("Toggle Extreme Optimization")]
      public void ToggleExtremeOptimization()
      {
            extremeOptimizationMode = !extremeOptimizationMode;
            Debug.Log($"Extreme optimization mode: {(extremeOptimizationMode ? "ENABLED" : "DISABLED")}");
            
            if (modelInitialized)
            {
                  ApplyResolutionBasedSettings();
            }
      }
      
      [ContextMenu("Toggle Model Processing")]
      public void ToggleModelProcessing()
      {
            pauseModelProcessing = !pauseModelProcessing;
            Debug.Log($"Model processing: {(pauseModelProcessing ? "PAUSED" : "RESUMED")}");
      }
      
      [ContextMenu("Force Model Run")]
      public void ForceModelRun()
      {
            if (modelInitialized && !pauseModelProcessing)
            {
                  lastRunTime = 0f; // Reset timer to force immediate run
                  Debug.Log("Forcing immediate model run...");
            }
      }
      
      public void SetMaxProcessingTime(float maxTimeMs)
      {
            maxModelProcessingTime = Mathf.Max(50f, maxTimeMs);
            Debug.Log($"Max processing time set to {maxModelProcessingTime}ms");
      }
      
      public void EnableAutoOptimization(bool enabled)
      {
            autoOptimizationEnabled = enabled;
            Debug.Log($"Auto-optimization: {(enabled ? "ENABLED" : "DISABLED")}");
      }
}