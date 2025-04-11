using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlyData", menuName = "ScriptableObjects/PlyData")]
public class splatObject : ScriptableObject
{
    [HideInInspector] public Vector3[] position;
    [HideInInspector] public Color[] color;
    [HideInInspector] public Vector3[] scale;
    [HideInInspector] public float[] opacity;
    [HideInInspector] public Quaternion[] rotation;
}
