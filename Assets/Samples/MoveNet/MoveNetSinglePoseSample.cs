using System.Reflection.PortableExecutable;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TensorFlowLite;
using TensorFlowLite.MoveNet;

[RequireComponent(typeof(WebCamInput))]
public class MoveNetSinglePoseSample : MonoBehaviour
{
    [SerializeField]
    MoveNetSinglePose.Options options = default;

    [SerializeField]
    private RectTransform cameraView = null;

    [SerializeField]
    private bool runBackground = false;

    [SerializeField, Range(0, 1)]
    private float threshold = 0.3f;

    private MoveNetSinglePose moveNet;
    private MoveNetPose pose;
    private MoveNetDrawer drawer;

    private UniTask<bool> task;
    private CancellationToken cancellationToken;

    private void Start()
    {
        moveNet = new MoveNetSinglePose(options);
        drawer = new MoveNetDrawer(Camera.main, cameraView);

        cancellationToken = this.GetCancellationTokenOnDestroy();

        var webCamInput = GetComponent<WebCamInput>();
        webCamInput.OnTextureUpdate.AddListener(OnTextureUpdate);
    }

    private void OnDestroy()
    {
        var webCamInput = GetComponent<WebCamInput>();
        webCamInput.OnTextureUpdate.RemoveListener(OnTextureUpdate);
        moveNet?.Dispose();
        drawer?.Dispose();
    }

     public static int IniCheckTh(float cos, float th)
        {
            if(cos <= th){
                return 1;
            }
            else{
                return 0;
           }
        }

        public static int SinkCheckTh(float cos, float th)
        {
            if(cos >= th){
                return 1;
            }
            else{
                return 0;
            }
        }

    private void Update()
    {
        if (pose != null)
        {
            drawer.DrawPose(pose, threshold);
            Debug.Log("左肩" + pose[5].x + "," + pose[5].y);
            Debug.Log("左手首" + pose[9].x + "," + pose[9].y);
            Debug.Log("左足首" + pose[15].x + "," + pose[15].y);
            
            float cos1 = 0.90;
            float cos2 = 0.975;
            float th1 = 0.91;
            float th2 = 0.98;

            if(IniCheckTh(cos1, th1) == 1){
                
                Debug.log("初期姿勢OK");
            }
            if(SinkCheckTh(cos2, th2) == 1){
                DebugDirectoryBuilder.log("最深姿勢OK");
            }
        }
    }

    private void OnTextureUpdate(Texture texture)
    {
        if (runBackground)
        {
            if (task.Status.IsCompleted())
            {
                task = InvokeAsync(texture);
            }
        }
        else
        {
            Invoke(texture);
        }
    }

    private void Invoke(Texture texture)
    {
        moveNet.Invoke(texture);
        pose = moveNet.GetResult();
    }

    private async UniTask<bool> InvokeAsync(Texture texture)
    {
        await moveNet.InvokeAsync(texture, cancellationToken);
        pose = moveNet.GetResult();
        return true;
    }
}
