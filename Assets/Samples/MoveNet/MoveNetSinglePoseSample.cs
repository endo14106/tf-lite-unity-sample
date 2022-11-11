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

    public static float CosCulc1 (float Ax, float Ay, float Bx, float By, float Cx, float Cy)
    {
        float CAx = Ax - Cx;
        float CAy = Ay - Cy;
        float CBx = Bx - Cx;
        float CBy = By - Cy;

        float cos = (CAx * CBx + CAy * CBy) / ((float)System.Math.Sqrt(CAx * CAx + CAy * CAy) * (float)System.Math.Sqrt(CBx * CBx + CBy * CBy));
        return cos;
    }

    public static void Count(float th_high, float th_low, float cos, int count, int achieve)
    {
        if(count == 0 && cos <= th_high)
        {
            count++;
            Debug.Log("count =" + count);
        }
        else if (count == 1 && cos >= th_low)
        {
            count++;
            Debug.Log("count =" + count);
        }
        else if (count == 2 && cos >= th_high)
        {
            count = 0;
            achieve++;
            Debug.Log("count =" + count + "achieve = " + achieve);
        }
    }

private float th_high = 0.91f;
private float th_low = 0.975f;
private int count = 1;
private int achieve = 0;


    private void Update()
    {
        if (pose != null)
        {
            drawer.DrawPose(pose, threshold);
            if (pose[5].score >= threshold && pose[9].score >= threshold && pose[15].score >= threshold)
            {
                float angle = CosCulc1(pose[9].x, pose[9].y, pose[5].x, pose[5].y, pose[15].x, pose[15].y);
               // Debug.Log("左肩" + pose[5].x + ", " + pose[5].y);
               // Debug.Log("左手首" + pose[9].x + ", " + pose[9].y);
               // Debug.Log("左足首" + pose[15].x + ", " + pose[15].y);
               Count(th_high, th_low, angle, count, achieve);
               //Debug.Log("cos =" + angle + "count =" + count + "achieve = " + achieve);
               //Debug.Log("count =" + count);
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
