Shader "Unlit/DrawSplat"
{
    Properties {
        _Wzmocnienie ("Wzmocnienie", float) = 1.5
        
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"

            StructuredBuffer<float3> splatPosition;
            StructuredBuffer<float3> splatScale;
            StructuredBuffer<float4> splatRotation;
            StructuredBuffer<float3> splatColor;
            StructuredBuffer<float>  splatOpacity;
            StructuredBuffer<float3>  gameObjectLocalPos;

            float _Wzmocnienie;

            struct appdata
            {
                uint vertexID : SV_VertexID; // tylko ID, nie potrzebujesz vertex.position
            };

            struct v2g
            {
                float3 pos      : TEXCOORD0;
                float3 scale    : TEXCOORD1;
                float4 rotation : TEXCOORD2;
                float3 color    : COLOR0;
                float  opacity  : TEXCOORD3;
            };

            v2g vert(appdata v)
            {
                uint id = v.vertexID;
                v2g o;
                o.pos = splatPosition[id];
    
                //o.pos.x+=gameObjectLocalPos[0];
                //o.pos.y+=gameObjectLocalPos[1];
                //o.pos.z+=gameObjectLocalPos[2];
                o.pos+=gameObjectLocalPos[0];

                o.scale = splatScale[id];
                o.rotation = splatRotation[id];
                o.color = splatColor[id];
                o.opacity = splatOpacity[id];
                return o;
            }

            struct g2f
            {
                float4 pos      : SV_POSITION;
                float3 scale    : TEXCOORD0;
                float4 rotation : TEXCOORD1;
                float3 color    : COLOR0;
                float  opacity  : TEXCOORD2;
            };

            float3x3 matrixMul(float3x3 a, float3x3 b)
            {
                float3x3 result = float3x3(
                    0,0,0,
                    0,0,0,
                    0,0,0);
                float r00 = a[0][0]* b[0][0]+    a[0][1]* b[1][0]+    a[0][2]* b[2][0];
                float r01 = a[0][0]* b[0][1]+    a[0][1]* b[1][1]+    a[0][2]* b[2][1];
                float r02 = a[0][0]* b[0][2]+    a[0][1]* b[1][2]+    a[0][2]* b[2][2];

                float r10 = a[1][0]* b[0][0]+    a[1][1]* b[1][0]+    a[1][2]* b[2][0];
                float r11 = a[1][0]* b[0][1]+    a[1][1]* b[1][1]+    a[1][2]* b[2][1];
                float r12 = a[1][0]* b[0][2]+    a[1][1]* b[1][2]+    a[1][2]* b[2][2];

                float r20 = a[2][0]* b[0][0]+    a[2][1]* b[1][0]+    a[2][2]* b[2][0];
                float r21 = a[2][0]* b[0][1]+    a[2][1]* b[1][1]+    a[2][2]* b[2][1];
                float r22 = a[2][0]* b[0][2]+    a[2][1]* b[1][2]+    a[2][2]* b[2][2];

                result = float3x3(
                    r00,r01,r02,
                    r10,r11,r12,
                    r20,r21,r22
                );
                return result;
            }

            float2x2 screenMatrix2D(float3 scale, float4 rotation){

                float2x2 matrix2D;

                //skala na macierz
                float3x3 scaleMatrix=float3x3(
                    scale.x,    0,      0,
                    0,      scale.y,    0,
                    0,          0,   scale.z
                );
                float3x3 scaleMatrixT=transpose(scaleMatrix);

                //rotacja na macierz
                float w,x,z,y;
                w = rotation.w;
                x = rotation.x;
                y = rotation.y;
                z = rotation.z;
                float3x3 rotationMatrix = float3x3(
                    1 - 2*(y*y + z*z),      2*(x*y - w*z),          2*(x*z+w*y),
                    2*(x*y + w*z),          1 -2*(x*x+z*z),         2*(y*z - w*x),
                    2*(x*z - w*y),          2*(y*z + w*x),          1-2*(x*x + y*y)
                );
                
                float3x3 rotationMatrixT = transpose(rotationMatrix);
                //macierz kowariancji
                float3x3 RSrot = matrixMul(rotationMatrix,scaleMatrix);
                float3x3 RSStrot = matrixMul(RSrot,scaleMatrixT);
                float3x3 covMatrix = matrixMul(RSStrot,rotationMatrixT);

                //macierz 2d z uproszczonym jakobianem (view right i view Up)
                float3 VHoriz =  UNITY_MATRIX_IT_MV[0].xyz;
                float3 VVert = UNITY_MATRIX_IT_MV[1].xyz;

                //J*macierz
                float3x3 mat3x2 =float3x3(
                    VHoriz.x*covMatrix[0][0]+ VHoriz.y*covMatrix[1][0]+VHoriz.z*covMatrix[2][0],    VHoriz.x*covMatrix[0][1]+ VHoriz.y*covMatrix[1][1]+VHoriz.z*covMatrix[2][1],    VHoriz.x*covMatrix[0][2]+ VHoriz.y*covMatrix[1][2]+VHoriz.z*covMatrix[2][2],
                    VVert.x*covMatrix[0][0]+ VVert.y*covMatrix[1][0]+VVert.z*covMatrix[2][0],       VVert.x*covMatrix[0][1]+ VVert.y*covMatrix[1][1]+VVert.z*covMatrix[2][1],       VVert.x*covMatrix[0][2]+ VVert.y*covMatrix[1][2]+VVert.z*covMatrix[2][2],
                    0,0,0
                );
                
                matrix2D = float2x2(
                    mat3x2[0][0]*VHoriz.x + mat3x2[0][1]*VHoriz.y + mat3x2[0][2]*VHoriz.z,  mat3x2[0][0]*VVert.x + mat3x2[0][1]*VVert.y + mat3x2[0][2]*VVert.z,
                    mat3x2[1][0]*VHoriz.x + mat3x2[1][1]*VHoriz.y + mat3x2[1][2]*VHoriz.z,  mat3x2[1][0]*VVert.x + mat3x2[1][1]*VVert.y + mat3x2[1][2]*VVert.z
                );

                return matrix2D;
            }


            float2 eigenVector(float2x2 mat2D, float eigenVal1)
            {
                float2 eigenVec = float2(0,0);
                float x=0;
                float y=0;
                if(mat2D[0][0]!=0)
                {
                     y=1;
                     x = -1*mat2D[0][1]*y/(mat2D[0][0]-eigenVal1);
                    //normalizacja
                    x = x/(sqrt(x*x + y*y));
                    y = y/(sqrt(x*x + y*y));
                }else
                {
                     x=1;
                     y = -1*mat2D[0][1]/(mat2D[0][0]-eigenVal1)*x;
                    //normalizacja
                    x = x/(sqrt(x*x + y*y));
                    y = y/(sqrt(x*x + y*y));
                }

                eigenVec= float2(x,y);

                return eigenVec;

            }


            [maxvertexcount(6)]
            void geom(point v2g input[1], inout TriangleStream<g2f> triStream)
            {
                 float rest0 = 0.2820948f;
                 float opac = 0.5f+ rest0*input[0].opacity;
   

                g2f o;
                 float4 color= (float4(input[0].color.xyz,    input[0].opacity)+1)*0.5;
                o.color= input[0].color;
                o.scale = input[0].scale;
                o.rotation = input[0].rotation;
                
                o.opacity =saturate(exp(input[0].opacity)*_Wzmocnienie); //mnożone przez 1.5 dla wzmocnienia widocznosci

                //screenMatrix
                float2x2 sMatrix = screenMatrix2D(o.scale,o.rotation);

               
                float delta=(sMatrix[0][0]+sMatrix[1][1])*(sMatrix[0][0]+sMatrix[1][1])- 4*(sMatrix[0][0]*sMatrix[1][1] - sMatrix[1][0]*sMatrix[0][1] );
                float lam1 = (sMatrix[0][0]+sMatrix[1][1] - sqrt(delta))/2;
                float lam2 = (sMatrix[0][0]+sMatrix[1][1] + sqrt(delta))/2;

                float scaleX = sqrt(abs(lam2))/100;
                float scaleY = sqrt(abs(lam1))/100;

                float2 eigenVec1 = eigenVector(sMatrix,lam1);
                float2 eigenVec2 = eigenVector(sMatrix,lam2);
                
                float3 right = normalize(UNITY_MATRIX_IT_MV[0].xyz)  * scaleX;
                float3 up    = normalize(UNITY_MATRIX_IT_MV[1].xyz) * scaleY;

               
                
                float3 pkt1 = input[0].pos  - right + up;
                float3 pkt2 = input[0].pos  + right + up;
                float3 pkt3 = input[0].pos  + right - up;
                float3 pkt4 = input[0].pos  - right - up;
                /*
                float2 axis1 = eigenVec1 * sqrt(lam1);
                float2 axis2 = eigenVec2 * sqrt(lam2);

                float3 axis1_3D = eigenVec1.x * right + eigenVec1.y * up;
                float3 axis2_3D = eigenVec2.x * right + eigenVec2.y * up;

                float3 pkt1 = input[0].pos  - right*axis1_3D.x +  up*axis2_3D.y;
                float3 pkt2 = input[0].pos  + right*axis1_3D.x +  up*axis2_3D.y;
                float3 pkt3 = input[0].pos  + right*axis1_3D.x -  up*axis2_3D.y;
                float3 pkt4 = input[0].pos  - right*axis1_3D.x -  up*axis2_3D.y;
                */
                o.pos = UnityObjectToClipPos(pkt1);
                triStream.Append(o);
                o.pos = UnityObjectToClipPos(pkt2);
                triStream.Append(o);
                o.pos = UnityObjectToClipPos(pkt3);
                triStream.Append(o);
                
                triStream.RestartStrip();
                o.color= input[0].color;
                o.scale = input[0].scale;
                o.rotation = input[0].rotation;
                o.opacity =saturate(exp(input[0].opacity)*_Wzmocnienie);

                o.pos = UnityObjectToClipPos(pkt1);
                triStream.Append(o);
                o.pos = UnityObjectToClipPos(pkt3);
                triStream.Append(o);
                o.pos = UnityObjectToClipPos(pkt4);
                triStream.Append(o);
            };

            float4 frag(g2f i) : SV_Target
            {
                //i.opacity = saturate(exp(i.opacity));
                return float4(i.color.xyz,i.opacity);
            }

            ENDCG
        }
    }
}
