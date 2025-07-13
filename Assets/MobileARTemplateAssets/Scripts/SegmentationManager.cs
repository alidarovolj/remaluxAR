using System;
using System.Collections;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SegmentationManager : MonoBehaviour
{
      [Header("AR")]
      [SerializeField]
      private ARCameraManager cameraManager;

      [Header("UI")]
      [SerializeField]
      private RawImage rawImage;

      [Header("ML Model")]
      [Tooltip("How often to run the model in seconds. A lower number is more responsive but more performance-intensive.")]
      [SerializeField]
      private float runSchedule = 0.1f;

      [Tooltip("The color to use for painting segmented objects.")]
      public Color paintColor = Color.blue;

      [Tooltip("The model to use for segmentation. This must be a .onnx file.")]
      [SerializeField]
      private NNModel modelAsset;
      private const string ModelName = "model.onnx";

      // This field is no longer needed as post-processing is done on the CPU.
      // [Tooltip("The compute file to use for post-processing. This must be a .compute file.")]
      // [SerializeField]
      // private ComputeShader postProcessShader;

      // TODO: The size of the image the model expects.
      // You might need to change this depending on the model you are using.
      private readonly Vector2Int imageSize = new Vector2Int(256, 256);

      private IWorker worker;
      private Model model;

      // Texture to hold the segmentation mask
      private Texture2D segmentationTexture;

      // The last output from the model
      private Tensor lastOutputTensor;

      // The class index of the object to be painted
      private int classIndexToPaint = -1; // -1 means nothing is selected for painting

      // Timer for scheduling model runs
      private float lastRunTime;

      private void Start()
      {
            // Ensure the AR Camera background is enabled and uses its default material
            var arCameraBackground = cameraManager.GetComponent<ARCameraBackground>();
            if (arCameraBackground != null)
            {
                  arCameraBackground.useCustomMaterial = false;
                  arCameraBackground.enabled = true;
            }

            // Load the model
            model = ModelLoader.Load(modelAsset);
            worker = model.CreateWorker();
            Application.targetFrameRate = 60;
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

            worker.Execute(inputTensor);

            // Get the output and dispose of the previous one
            lastOutputTensor?.Dispose();
            lastOutputTensor = worker.PeekOutput();

            // On the first run, initialize the texture with the correct size from the model output
            if (segmentationTexture == null || segmentationTexture.width != lastOutputTensor.width || segmentationTexture.height != lastOutputTensor.height)
            {
                  segmentationTexture = new Texture2D(lastOutputTensor.width, lastOutputTensor.height, TextureFormat.RGBA32, false)
                  {
                        filterMode = FilterMode.Bilinear
                  };
                  rawImage.texture = segmentationTexture;

                  // Log model output details once to help with debugging
                  Debug.Log($"Model output initialized. Texture size: {lastOutputTensor.width}x{lastOutputTensor.height}, Channels/Classes: {lastOutputTensor.channels}");
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

            // Clean up
            inputTensor.Dispose();

            yield return null;
      }

      void Update()
      {
            // Check for screen tap
            if (Input.GetMouseButtonDown(0))
            {
                  HandleTap(Input.mousePosition);
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

                        // Set the class index to be painted
                        classIndexToPaint = classIndex;
                  }
                  else
                  {
                        Debug.LogWarning($"Tapped on an object with an invalid class index: {classIndex}.");
                  }
            }
      }
}