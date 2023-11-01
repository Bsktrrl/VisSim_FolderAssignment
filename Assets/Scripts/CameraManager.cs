using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class CameraManager : MonoBehaviour
{
    [SerializeField] Camera camera;


    //--------------------


    private void Start()
    {
        Perspective1();

        camera.targetDisplay = 0;
    }


    //--------------------


    public void Perspective1()
    {
        camera.transform.position = new Vector3(790, 777, 130);
        camera.transform.rotation = Quaternion.identity;
        camera.transform.Rotate(new Vector3(64, 0, 0), Space.World);
    }
    public void Perspective2()
    {
        camera.transform.position = new Vector3(795, 315, -394);
        camera.transform.rotation = Quaternion.identity;
        camera.transform.Rotate(new Vector3(15, 0, 0), Space.World);
    }
    public void Perspective3()
    {
        camera.transform.position = new Vector3(795, 327, 1601);
        camera.transform.rotation = Quaternion.identity;
        camera.transform.Rotate(new Vector3(16, 180, 0), Space.World);
    }
    public void Perspective4()
    {
        camera.transform.position = new Vector3(1875, 276, 672);
        camera.transform.rotation = Quaternion.identity;
        camera.transform.Rotate(new Vector3(10, 266, 1.5f), Space.World);
    }
    public void Perspective5()
    {
        camera.transform.position = new Vector3(-259, 237, 597);
        camera.transform.rotation = Quaternion.identity;
        camera.transform.Rotate(new Vector3(5, 90, 0), Space.World);
    }
}
