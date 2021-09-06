﻿using System.Threading;
using Cysharp.Threading.Tasks;
using TensorFlowLite;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BlazePose form MediaPipe
/// https://github.com/google/mediapipe
/// https://viz.mediapipe.dev/demo/pose_tracking
/// </summary>
public sealed class BlazePoseSample : MonoBehaviour
{

    [SerializeField, FilePopup("*.tflite")] string poseDetectionModelFile = "coco_ssd_mobilenet_quant.tflite";
    [SerializeField, FilePopup("*.tflite")] string poseLandmarkModelFile = "coco_ssd_mobilenet_quant.tflite";
    [SerializeField] RawImage cameraView = null;
    [SerializeField] RawImage debugView = null;
    [SerializeField] Canvas canvas = null;
    [SerializeField] bool runBackground;
    [SerializeField, Range(0f, 1f)] float visibilityThreshold = 0.5f;
    [SerializeField] PoseLandmarkDetect.Options landmarkOptions = new PoseLandmarkDetect.Options();

    WebCamTexture webcamTexture;
    PoseDetect poseDetect;
    PoseLandmarkDetect poseLandmark;

    Vector3[] rtCorners = new Vector3[4]; // just cache for GetWorldCorners

    Vector4[] worldLandmarks;
    PrimitiveDraw draw;
    PoseDetect.Result poseResult;
    PoseLandmarkDetect.Result landmarkResult;
    UniTask<bool> task;
    CancellationToken cancellationToken;

    bool NeedsDetectionUpdate => poseResult == null || poseResult.score < 0.5f;

    void Start()
    {
        // Init model
        poseDetect = new PoseDetect(poseDetectionModelFile);
        poseLandmark = new PoseLandmarkDetect(poseLandmarkModelFile, landmarkOptions);

        // Init camera 
        string cameraName = WebCamUtil.FindName(WebCamKind.WideAngle, false);
        webcamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
        cameraView.texture = webcamTexture;
        webcamTexture.Play();
        Debug.Log($"Starting camera: {cameraName}");

        draw = new PrimitiveDraw(Camera.main, gameObject.layer);
        worldLandmarks = new Vector4[PoseLandmarkDetect.LandmarkCount];

        cancellationToken = this.GetCancellationTokenOnDestroy();
    }

    void OnDestroy()
    {
        webcamTexture?.Stop();
        poseDetect?.Dispose();
        poseLandmark?.Dispose();
        draw?.Dispose();
    }

    void Update()
    {
        if (runBackground)
        {
            if (task.Status.IsCompleted())
            {
                task = InvokeAsync();
            }
        }
        else
        {
            Invoke();
        }

        if (poseResult != null && poseResult.score > 0f)
        {
            DrawFrame(poseResult);
        }

        if (landmarkResult != null && landmarkResult.score > 0.2f)
        {
            DrawCropMatrix(poseLandmark.CropMatrix);
            DrawLandmarks(landmarkResult.viewportLandmarks);
        }
    }

    void DrawFrame(PoseDetect.Result pose)
    {
        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        draw.color = Color.green;
        draw.Rect(MathTF.Lerp(min, max, pose.rect, true), 0.02f, min.z);

        foreach (var kp in pose.keypoints)
        {
            draw.Point(MathTF.Lerp(min, max, (Vector3)kp, true), 0.05f);
        }
        draw.Apply();
    }

    void DrawCropMatrix(in Matrix4x4 matrix)
    {
        draw.color = Color.red;

        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        var mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, false, webcamTexture.videoVerticallyMirrored)
            * matrix.inverse;
        Vector3 a = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 0, 0)));
        Vector3 b = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 0, 0)));
        Vector3 c = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 1, 0)));
        Vector3 d = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 1, 0)));

        draw.Quad(a, b, c, d, 0.02f);
        draw.Apply();
    }

    void DrawLandmarks(Vector4[] landmarks)
    {
        draw.color = Color.blue;

        // Vector3 min = rtCorners[0];
        // Vector3 max = rtCorners[2];
        // Debug.Log($"rtCorners min: {min}, max: {max}");

        // Apply webcam rotation to draw landmarks correctly
        Matrix4x4 mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, false, webcamTexture.videoVerticallyMirrored);

        // float zScale = (max.x - min.x) / 2;
        float zScale = 1;
        float zOffset = canvas.planeDistance;
        float aspect = (float)Screen.width / (float)Screen.height;
        Vector3 scale, offset;
        if (aspect > 1)
        {
            scale = new Vector3(1f / aspect, 1f, zScale);
            offset = new Vector3((1 - 1f / aspect) / 2, 0, zOffset);
        }
        else
        {
            scale = new Vector3(1f, aspect, zScale);
            offset = new Vector3(0, (1 - aspect) / 2, zOffset);
        }

        // Update world joints
        var camera = canvas.worldCamera;
        for (int i = 0; i < landmarks.Length; i++)
        {
            Vector3 p = mtx.MultiplyPoint3x4((Vector3)landmarks[i]);
            p = Vector3.Scale(p, scale) + offset;
            p = camera.ViewportToWorldPoint(p);

            // w is visibility
            worldLandmarks[i] = new Vector4(p.x, p.y, p.z, landmarks[i].w);
        }

        // Draw
        for (int i = 0; i < worldLandmarks.Length; i++)
        {
            Vector4 p = worldLandmarks[i];
            if (p.w > visibilityThreshold)
            {
                draw.Cube(p, 0.2f);
            }
        }
        var connections = PoseLandmarkDetect.Connections;
        for (int i = 0; i < connections.Length; i += 2)
        {
            var a = worldLandmarks[connections[i]];
            var b = worldLandmarks[connections[i + 1]];
            if (a.w > visibilityThreshold || b.w > visibilityThreshold)
            {
                draw.Line3D(a, b, 0.05f);
            }
        }
        draw.Apply();
    }

    void Invoke()
    {
        if (NeedsDetectionUpdate)
        {
            poseDetect.Invoke(webcamTexture);
            cameraView.material = poseDetect.transformMat;
            cameraView.rectTransform.GetWorldCorners(rtCorners);
            poseResult = poseDetect.GetResults(0.7f, 0.3f);
        }
        if (poseResult.score < 0)
        {
            poseResult = null;
            landmarkResult = null;
            return;
        }
        poseLandmark.Invoke(webcamTexture, poseResult);
        debugView.texture = poseLandmark.inputTex;

        landmarkResult = poseLandmark.GetResult();

        if (landmarkResult.score < 0.3f)
        {
            poseResult.score = landmarkResult.score;
        }
        else
        {
            poseResult = PoseLandmarkDetect.LandmarkToDetection(landmarkResult);
        }
    }

    async UniTask<bool> InvokeAsync()
    {
        if (NeedsDetectionUpdate)
        {
            // Note: `await` changes PlayerLoopTiming from Update to FixedUpdate.
            poseResult = await poseDetect.InvokeAsync(webcamTexture, cancellationToken, PlayerLoopTiming.FixedUpdate);
        }
        if (poseResult.score < 0)
        {
            poseResult = null;
            landmarkResult = null;
            return false;
        }

        landmarkResult = await poseLandmark.InvokeAsync(webcamTexture, poseResult, cancellationToken, PlayerLoopTiming.Update);

        // Back to the update timing from now on 
        if (cameraView != null)
        {
            cameraView.material = poseDetect.transformMat;
            cameraView.rectTransform.GetWorldCorners(rtCorners);
        }
        if (debugView != null)
        {
            debugView.texture = poseLandmark.inputTex;
        }

        // Generate poseResult from landmarkResult
        if (landmarkResult.score < 0.3f)
        {
            poseResult.score = landmarkResult.score;
        }
        else
        {
            poseResult = PoseLandmarkDetect.LandmarkToDetection(landmarkResult);
        }

        return true;
    }
}
