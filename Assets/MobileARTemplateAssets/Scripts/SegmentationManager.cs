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

      [Header("ML Model")]
      [Tooltip("How often to run the model in seconds. A lower number is more responsive but more performance-intensive.")]
      [SerializeField]
      private float runSchedule = 0.2f;

      [Tooltip("The color to use for painting segmented objects.")]
      public Color paintColor = Color.blue;

      [Tooltip("The model to use for segmentation. This can be an .onnx or .tflite file.")]
      [SerializeField]
      private NNModel modelAsset;

      // These are now set dynamically in Start() based on the selected model
      private Vector2Int imageSize;

      private IWorker worker;
      private Model model;

      // Texture to hold the segmentation mask
      private Texture2D segmentationTexture;

      // The last output from the model
      private Tensor lastOutputTensor;

      // The class index of the object to be painted
      private int classIndexToPaint = -1; // -1 means nothing is selected for painting, show all classes

      private Coroutine displayNameCoroutine;

      // Timer for scheduling model runs
      private float lastRunTime;

      // Frame skipping for performance
      private int frameCounter = 0;
      private const int frameSkip = 0; // Process every frame for better responsiveness

      // Flag to prevent repeated logging
      private bool hasLoggedStatistics = false;

      // For double-tap detection
      private float lastTapTime = 0f;
      private const float doubleTapThreshold = 0.5f;

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

            // --- DYNAMIC SETTINGS BASED ON MODEL ---
            // Automatically configure settings based on the model assigned in the Inspector
            if (modelAsset.name.Contains("TopFormer"))
            {
                  Debug.Log("TopFormer model detected. Adjusting settings for the high-resolution model.");
                  imageSize = new Vector2Int(512, 512); // TopFormer requires 512x512
                  runSchedule = 0.5f; // Faster updates - reduced from 1.0f to 0.5f
                  Application.targetFrameRate = 25; // Slightly higher FPS
                  Debug.LogWarning("IMPORTANT: For TopFormer, ensure your ColorMap.cs uses ADE20K dataset classes!");
            }
            else // Default to settings for a lighter model like CamVid
            {
                  Debug.Log("Lightweight model detected (e.g., CamVid). Using performance-oriented settings.");
                  imageSize = new Vector2Int(224, 224); // CamVid uses 224x224
                  runSchedule = 0.2f; // Faster updates
                  Application.targetFrameRate = 30; // Higher FPS is fine
                  Debug.LogWarning("IMPORTANT: For this model, ensure your ColorMap.cs uses CamVid dataset classes!");
            }

            // Load the model
            model = ModelLoader.Load(modelAsset);

            // Log model information for debugging
            Debug.Log($"Model loaded: {modelAsset.name}. Using input size {imageSize.x}x{imageSize.y}");
            foreach (var input in model.inputs)
            {
                  Debug.Log($"Model input: {input.name}, shape: {input.shape}");
            }
            foreach (var output in model.outputs)
            {
                  Debug.Log($"Model output: {output}");
            }

            worker = model.CreateWorker();

            QualitySettings.vSyncCount = 0;   // Disable VSync for better performance

            // Show all classes by default
            classIndexToPaint = -1;
      }

      private void OnEnable()
      {
            cameraManager.frameReceived += OnCameraFrameReceived;
      }

      private void OnDisable()
      {
            cameraManager.frameReceived -= OnCameraFrameReceived;
      }

      private void OnDestroy()
      {
            worker?.Dispose();
            lastOutputTensor?.Dispose();
            Destroy(segmentationTexture);
      }

      private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
      {
            // Skip frames for better performance
            frameCounter++;
            if (frameCounter <= frameSkip)
            {
                  return;
            }
            frameCounter = 0;

            if (Time.time - lastRunTime < runSchedule)
            {
                  return;
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

            var texture = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, conversionParams.outputFormat, false);
            var buffer = texture.GetRawTextureData<byte>();

            try
            {
                  cpuImage.Convert(conversionParams, buffer);
            }
            finally
            {
                  cpuImage.Dispose();
            }

            texture.Apply();
            StartCoroutine(RunModel(texture));
      }

      private IEnumerator RunModel(Texture2D texture)
      {
            using var inputTensor = new Tensor(texture, 3);

            try
            {
                  worker.Execute(inputTensor);

                  // Get the output and dispose of the previous one
                  lastOutputTensor?.Dispose();
                  lastOutputTensor = worker.PeekOutput();

                  // On the first run, initialize the texture with the correct size from the model output
                  if (segmentationTexture == null)
                  {
                        segmentationTexture = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false)
                        {
                              filterMode = FilterMode.Bilinear
                        };
                        rawImage.texture = segmentationTexture;

                        // Log model output details once to help with debugging
                        Debug.Log($"Model output initialized. Texture size: {lastOutputTensor.width}x{lastOutputTensor.height}, Channels/Classes: {lastOutputTensor.channels}");

                        // Debug: Show which classes are most common in the current frame (only once)
                        if (!hasLoggedStatistics)
                        {
                              LogClassStatistics();
                              hasLoggedStatistics = true;
                        }
                  }

                  // Process the output and update the texture
                  SegmentationPostProcessing.ProcessOutput(lastOutputTensor, segmentationTexture, classIndexToPaint, paintColor);

                  // Adjust RawImage aspect ratio to match the screen
                  var screenAspect = (float)Screen.width / Screen.height;
                  var imageAspect = (float)segmentationTexture.width / segmentationTexture.height;

                  if (screenAspect > imageAspect)
                  {
                        rawImage.rectTransform.localScale = new Vector3(imageAspect / screenAspect, 1, 1);
                  }
                  else
                  {
                        rawImage.rectTransform.localScale = new Vector3(1, screenAspect / imageAspect, 1);
                  }
            }
            catch (Exception e)
            {
                  Debug.LogError($"Model execution failed: {e.Message}");
                  if (inputTensor != null)
                  {
                        Debug.LogError($"Input tensor shape: {inputTensor.shape}");
                  }
                  Debug.LogError("Try adjusting the imageSize to match model requirements or assign a different model.");
            }

            // Clean up
            inputTensor.Dispose();

            yield return null;
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
      }

      void Update()
      {
            // Check for screen tap
            if (Input.GetMouseButtonDown(0))
            {
                  float currentTime = Time.time;

                  // Check for double tap to clear selection
                  if (currentTime - lastTapTime < doubleTapThreshold)
                  {
                        // Double tap detected - clear selection
                        classIndexToPaint = -1;
                        Debug.Log("Selection cleared. Showing all classes.");

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
                        Debug.Log($"You tapped on: {className} (class index: {classIndex}). Painting it.");

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
}