using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;

[ExecuteAlways]
public class renderSplat : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] splatObject splaty;
    public Shader shader;
    public Material material;
    [SerializeField]
    int VisibleSplats;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer scaleBuffer;
    private ComputeBuffer colorBuffer;
    private ComputeBuffer rotationBuffer;
    private ComputeBuffer opacityBuffer;
    private ComputeBuffer localPositionBuffer;
    ComputeBuffer splatNumberBuffer;

    public void loadBufor()
    {
        int numberOffields = 14;
        if (splaty.position.Count() > 0)
        {
            splatNumberBuffer = new ComputeBuffer(1, sizeof(float));
            float[] floats = { numberOffields };
            splatNumberBuffer.SetData(floats);

            //ComputeBuffer sBuffer = new ComputeBuffer(splaty.position.Count() * numberOffields,sizeof(float));
            positionBuffer = new ComputeBuffer(splaty.position.Count(), sizeof(float) * 3);
            scaleBuffer = new ComputeBuffer(splaty.position.Count(), sizeof(float) * 3);
            colorBuffer = new ComputeBuffer(splaty.position.Count(), sizeof(float) * 3);
            rotationBuffer = new ComputeBuffer(splaty.position.Count(), sizeof(float) * 4);
            opacityBuffer = new ComputeBuffer(splaty.position.Count(), sizeof(float));
            localPositionBuffer = new ComputeBuffer(3, sizeof(float) * 3);

            //List <float> loadDataToBuffer= new List<float>();
            List<Vector3> positionList = new List<Vector3>();
            List<Vector3> scaleList = new List<Vector3>();
            List<Vector3> colorList = new List<Vector3>();
            List<Vector4> rotationList = new List<Vector4>();
            List<float> opacityList = new List<float>();

            if (VisibleSplats <0) VisibleSplats = 100;
            if(VisibleSplats> splaty.position.Count())VisibleSplats = splaty.position.Count()-1;

            for (int i = 0; i < VisibleSplats; i++)
            {
                Vector3 pos = splaty.position[i];
                pos.y *= -1;
                positionList.Add(pos);
                colorList.Add(new Vector3(splaty.color[i].r, splaty.color[i].g, splaty.color[i].b));
                opacityList.Add(splaty.color[i].a);
                scaleList.Add(splaty.scale[i]);
                rotationList.Add(new Vector4(splaty.rotation[i].x, splaty.rotation[i].y, splaty.rotation[i].z, splaty.rotation[i].w));
            }

            for (int i = 0; i < 10; i++) 
            {
                Debug.Log("Opacity: "+ opacityList[i].ToString());
                Debug.Log("Changed opacity: "+Mathf.Clamp(Mathf.Exp(opacityList[i]), 0.0f, 1.0f));
            }
            
            Debug.Log("Liczba splatów dla bufera: " + splaty.position.Count());
            
            Vector3[] floatsPosition = positionList.ToArray();
            Vector3[] floatsScale = scaleList.ToArray();
            Vector4[] floatsRotation = rotationList.ToArray();
            Vector3[] floatsColor = colorList.ToArray();
            Vector3[] floatLocalPos =new Vector3[]{transform.position};
            float[] floatsOpacity = opacityList.ToArray();

            positionBuffer.SetData(floatsPosition);
            scaleBuffer.SetData(floatsScale);
            rotationBuffer.SetData(floatsRotation);
            colorBuffer.SetData(floatsColor);
            opacityBuffer.SetData(floatsOpacity);
            localPositionBuffer.SetData(floatLocalPos);



            //material.SetBuffer("splatData",sBuffer);
            material.SetBuffer("splatPosition", positionBuffer);
            material.SetBuffer("splatScale", scaleBuffer);
            material.SetBuffer("splatRotation", rotationBuffer);
            material.SetBuffer("splatColor", colorBuffer);
            material.SetBuffer("splatOpacity", opacityBuffer);
            material.SetBuffer("splatNumber", splatNumberBuffer);
            material.SetBuffer("gameObjectLocalPos", localPositionBuffer);

        }
    }
    public void renderSplats()
    {
                   
        gameObject.GetComponent<MeshRenderer>().material = material;
        material.SetPass(0); // <- Ustawia shader do renderowania
        Graphics.DrawProceduralNow(MeshTopology.Points, VisibleSplats);//positionBuffer.count);

    }

    private void Start()
    {
        loadBufor();

    }

    private void OnRenderObject()
    {
        if ((shader != null) && (splaty.position.Count() > 0))
        {
            renderSplats();
        }
    }
    public void Update()
    {
        if (localPositionBuffer != null && material != null)
        {
            Vector3[] floatLocalPos = new Vector3[]{ transform.position};
            localPositionBuffer.SetData(floatLocalPos);
            material.SetBuffer("gameObjectLocalPos", localPositionBuffer);
        }

        if (localPositionBuffer == null) 
        {
            loadBufor();
        }
        
    }
    private void OnEnable()
    {
        loadBufor();
    }

   

    private void OnDisable()
    {
        positionBuffer?.Release();
        scaleBuffer?.Release();
        rotationBuffer?.Release();
        colorBuffer?.Release();
        opacityBuffer?.Release();
        splatNumberBuffer?.Release();
        localPositionBuffer?.Release();
    }

    private void OnDestroy()
    {
        positionBuffer?.Release();
        scaleBuffer?.Release();
        rotationBuffer?.Release();
        colorBuffer?.Release();
        opacityBuffer?.Release();
        splatNumberBuffer?.Release();
        localPositionBuffer?.Release();
    }
}
