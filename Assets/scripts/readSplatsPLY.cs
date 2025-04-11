#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using UnityEngine.UIElements;
using Unity.Mathematics;

public class readSplatsPLY : MonoBehaviour
{
    
    [MenuItem("Tools/Import Gaussian .ply")]
    static void ImportPly()
    {
        string path = EditorUtility.OpenFilePanel("Select binary .ply file", "", "ply");
        if (string.IsNullOrEmpty(path)) return;

        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(fs))
        {
            List<string> headerLines = new List<string>();
            string line;
            while ((line = ReadLine(reader)) != "end_header")
            {
                headerLines.Add(line);
            }

            // Parse header to get order of properties
            List<string> properties = new List<string>();
            int vertexCount = 0;

            foreach (var hl in headerLines)
            {
                if (hl.StartsWith("element vertex"))
                {
                    vertexCount = int.Parse(hl.Split(' ')[2]);
                }
                else if (hl.StartsWith("property"))
                {
                    properties.Add(hl.Split(' ')[2]);
                }
            }

            var positions = new List<Vector3>();
            var colors = new List<Color>();
            var opacities = new List<float>();
            var scales = new List<Vector3>();
            var rotations = new List<Quaternion>();

            for (int i = 0; i < vertexCount; i++)
            {
                
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                float nx = reader.ReadSingle(); // skip normal
                float ny = reader.ReadSingle();
                float nz = reader.ReadSingle();
                float f_dc_0 = reader.ReadSingle();
                float f_dc_1 = reader.ReadSingle();
                float f_dc_2 = reader.ReadSingle();
                float rest_0 = reader.ReadSingle();
                Debug.Log(rest_0);

                //reader.BaseStream.Seek(45, SeekOrigin.Current);
                for (int j = 0; j < 44; j++)
                {
                    float notneeded = reader.ReadSingle();
                    if (i == 1)
                    {
                        Debug.Log("f rest: " + notneeded);
                    }
                }
                float opacity = reader.ReadSingle();
                float scale_0 = reader.ReadSingle();
                float scale_1 = reader.ReadSingle();
                float scale_2 = reader.ReadSingle();
                float rot_0 = reader.ReadSingle();
                float rot_1 = reader.ReadSingle();
                float rot_2 = reader.ReadSingle();
                float rot_3 = reader.ReadSingle();

                if (i == 1)
                {
                    Debug.Log("Wartoœci: " +
                        " opsacity " + opacity +
                        " scale_0 " + scale_0 +
                        " scale_1 " + scale_1 +
                        " scale_2 " + scale_2 +
                        " rot_0 " + rot_0 +
                        " rot_1 " + rot_1 +
                        " rot_2" + rot_2 +
                        " rot_3 " + rot_3);
                }

                positions.Add(new Vector3(x, y, z));
                Color col = convertColorFromPLYToRGB(new Color(f_dc_0, f_dc_1, f_dc_2), rest_0);

                colors.Add(new Color(col.r,col.g,col.b, opacity));
                opacities.Add(opacity);
                scales.Add(new Vector3(scale_0, scale_1, scale_2));
                rotations.Add(new Quaternion(rot_0, rot_1, rot_2, rot_3));
            }

            splatObject data = ScriptableObject.CreateInstance<splatObject>();
            data.position = positions.ToArray();
            data.color = colors.ToArray();
            data.opacity = opacities.ToArray();
            data.scale = scales.ToArray();
            data.rotation = rotations.ToArray();

            string savePath = EditorUtility.SaveFilePanelInProject(
                "Save Gaussian Splat Data",
                Path.GetFileNameWithoutExtension(path) + ".asset",
                "asset",
                "Choose location to save the generated asset"
            );

            if (!string.IsNullOrEmpty(savePath))
            {
                AssetDatabase.CreateAsset(data, savePath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Saved Gaussian splat data asset to: {savePath}");
                Debug.Log($" Zaimportowano {data.position.Length} splatów do assetu.");
            }
        }
       
    }

    static public Color convertColorFromPLYToRGB(Color color,float rest0) 
    {
        Color col=new Color();
        rest0 = 0.2820948f;
        col.r = 0.5f+ rest0*color.r;
        col.g = 0.5f + rest0 *color.g;
        col.b = 0.5f + rest0 *color.b;


        return col;
    } 

    static string ReadLine(BinaryReader reader)
    {
        List<byte> bytes = new List<byte>();
        byte b;
        while ((b = reader.ReadByte()) != 10) // ASCII LF '\n'
        {
            bytes.Add(b);
        }
        return Encoding.ASCII.GetString(bytes.ToArray());
    }
}
#endif