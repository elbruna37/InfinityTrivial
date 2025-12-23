using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ForceAspectRatio : MonoBehaviour
{
    public float targetAspect = 16f / 9f;

    int lastW, lastH;

    void Start()
    {
        ApplyAspect();
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH)
        {
            lastW = Screen.width;
            lastH = Screen.height;
            ApplyAspect();
        }
    }

    void ApplyAspect()
    {
        Camera cam = GetComponent<Camera>();

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            cam.rect = new Rect(
                0,
                (1.0f - scaleHeight) / 2.0f,
                1.0f,
                scaleHeight
            );
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            cam.rect = new Rect(
                (1.0f - scaleWidth) / 2.0f,
                0,
                scaleWidth,
                1.0f
            );
        }
    }

}
