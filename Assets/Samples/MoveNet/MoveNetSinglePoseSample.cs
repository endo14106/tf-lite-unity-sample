using System.Reflection.PortableExecutable;
using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TensorFlowLite;
using TensorFlowLite.MoveNet;
using Debug = UnityEngine.Debug;

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

    public static void Count(float th_high, float th_low, float th_gro, float cos, ref int count, ref int achieve)
    {
        //count = 0 : 初期状態　何もなし
        //count = 1 : スタート姿勢　
        //count = 2 : 最深状態　
        if(count == 0 && cos <= th_high)
        {
            count++;
            //Debug.Log("count =" + count);
        }
        else if (count == 1 && cos >= th_low)
        {
            count++;
            //Debug.Log("count =" + count);
        }
        else if (count == 2 /*&& cos <= th_high*/)
        {
            if(cos <= th_high){
                count = 0;
                achieve++;
            }
            else if(cos >= th_gro){
                count = 0;
            }
            //Debug.Log("count =" + count + "achieve = " + achieve);
        }
        else
        {
            //Debug.Log("abc");
        }
    }

    public static float SlopeSubAbs(float SH_x, float SH_y, float HI_x, float HI_y, float AN_x, float AN_y)
{
    float Slope_SH = (HI_x-SH_x)/(HI_y-SH_y);
    float Slope_HA = (AN_x-HI_x)/(AN_y-HI_y);

    return Math.Abs(Slope_SH-SlopeHA);
}

private float th_high = 0.93f;
private float th_low = 0.975f;
private float th_gro = 0.99f;
public int count = 0;
public int achieve = 0;


    private void Update()
    {
        if (pose != null)
        {   
            if (0 <= achieve && achieve <= 100){
                drawer.DrawPose(pose, threshold);
            }
            if (pose[5].score >= threshold && pose[9].score >= threshold && pose[15].score >= threshold)
            {
                float angle = CosCulc1(pose[9].x, pose[9].y, pose[5].x, pose[5].y, pose[15].x, pose[15].y);
                // Debug.Log("左肩" + pose[5].x + ", " + pose[5].y);
                // Debug.Log("左手首" + pose[9].x + ", " + pose[9].y);
                // Debug.Log("左足首" + pose[15].x + ", " + pose[15].y);
                float sub = SlopeSubAbs(pose[5].x, pose[5].y, pose[11].x, pose[11].y, pose[15].x, pose[15].y);
                // Debug.Log("左手首-左足首：" + sub);
                if(Math.Abs(sub) <= 0.035f){
                    Count(th_high, th_low, th_gro, angle, ref count, ref achieve);
                } else {
                    Debug.Log("aaa");
                }
                Debug.Log("cos =" + angle + "count =" + count + "achieve = " + achieve);
                Debug.Log("sub = " + sub);
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
