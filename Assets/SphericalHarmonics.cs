using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public delegate float SH_Base(Vector3 v);
public class SH9
{
    private float[] coefficients = new float[9];

    public float this[int index]
    {
        get
        {
            return coefficients[index];
        }
        set
        {
            coefficients[index] = value;
        }
    }

    private const float sh0 = 0.28209479f;

    // 1,-1 y  1,0 z  1,1 x
    private const float sh1 = 0.48860251f;

    // 2,-2 xy   
    private const float sh2_2 = 1.09254843f;

    // 2,-1 yz
    private const float sh2_1 = 1.09254843f;

    // 2,0 -x^2 - y^2 + 2z^2
    private const float sh20 = 0.31539157f;

    // 2, 1 zx
    private const float sh21 = 1.09254843f;

    // 2, 2 x^2 - y^2
    private const float sh22 = 0.54627421f;

 


   
    public static float[] shArray = { sh0, sh1, sh1, sh1, sh2_2, sh2_1, sh20, sh21, sh22 };
    public static SH9 GetBasis(Vector3 coord)
    {
        return GetBasis(coord.x, coord.y, coord.z);
    }

    public static SH9 GetBasis(float x, float y, float z)
    {
        SH9 res = new SH9();
        res[0] = sh0;
        res[1] = sh1 * y;
        res[2] = sh1 * z;
        res[3] = sh1 * x;
        res[4] = sh2_2 * x * y;
        res[5] = sh2_1 * y * z;
        res[6] = sh20 * (-x * x - y * y + 2 * z * z);
        res[7] = sh21 * z * x;
        res[8] = sh22 * (x * x - y * y);

       
        return res;
    }

}
     

public class SphericalHarmonics
{
    const float sqrtPI = 1.77245383f;
    const float fC0 = (1.0f / (2.0f * sqrtPI));
    const float fC1 = (1.73205080f / (3.0f * sqrtPI));
    const float fC2 = (3.8729833f / (8.0f * sqrtPI));
    const float fC3 = (2.236067977f / (16.0f * sqrtPI));
    const float fC4 = (0.5f * fC2);

    static float[] kNormalizationConstants
    {
        get { return new float[] { fC0, fC1, fC1, fC1, fC2, fC2, fC3, fC2, fC4 }; }
    }

    //Convert a RenderTextureFormat to TextureFormat
    public static TextureFormat ConvertFormat(RenderTextureFormat input_format)
    {
        TextureFormat output_format = TextureFormat.RGBA32;

        switch (input_format)
        {
            case RenderTextureFormat.ARGB32:
                output_format = TextureFormat.RGBA32;
                break;

            case RenderTextureFormat.ARGBHalf:
                output_format = TextureFormat.RGBAHalf;
                break;

            case RenderTextureFormat.ARGBFloat:
                output_format = TextureFormat.RGBAFloat;
                break;

            default:
                string format_string = System.Enum.GetName(typeof(RenderTextureFormat), input_format);
                int format_int = (int)System.Enum.Parse(typeof(TextureFormat), format_string);
                output_format = (TextureFormat)format_int;
                break;
        }

        return output_format;
    }

    //Convert a TextureFormat to RenderTextureFormat
    public static RenderTextureFormat ConvertRenderFormat(TextureFormat input_format)
    {
        RenderTextureFormat output_format = RenderTextureFormat.ARGB32;

        switch (input_format)
        {
            case TextureFormat.RGBA32:
                output_format = RenderTextureFormat.ARGB32;
                break;

            case TextureFormat.RGBAHalf:
                output_format = RenderTextureFormat.ARGBHalf;
                break;

            case TextureFormat.RGBAFloat:
                output_format = RenderTextureFormat.ARGBFloat;
                break;

            default:
                string format_string = System.Enum.GetName(typeof(TextureFormat), input_format);
                int format_int = (int)System.Enum.Parse(typeof(RenderTextureFormat), format_string);
                output_format = (RenderTextureFormat)format_int;
                break;
        }

        return output_format;
    }

    //Convert a RenderTexture to a Cubemap
    public static Cubemap RenderTextureToCubemap(RenderTexture input)
    {
        if (input.dimension != UnityEngine.Rendering.TextureDimension.Cube)
        {
            Debug.LogWarning("Input render texture dimension must be cube");
            return null;
        }

        if (input.width != input.height)
        {
            Debug.LogWarning("Input render texture must be square");
            return null;
        }

        Cubemap output = new Cubemap(input.width, ConvertFormat(input.format), false);
        Texture2D tmp_face = new Texture2D(input.width, input.height, output.format, false);

        RenderTexture active = RenderTexture.active;

        for (int face = 0; face < 6; ++face)
        {
            Graphics.SetRenderTarget(input, 0, (CubemapFace)face);
            tmp_face.ReadPixels(new Rect(0, 0, input.width, input.height), 0, 0);
            output.SetPixels(tmp_face.GetPixels(), (CubemapFace)face);
        }
        output.Apply();

        RenderTexture.active = active;

        Object.DestroyImmediate(tmp_face);

        return output;
    }
 

 
    internal static void AddDirectionalLight(ref SphericalHarmonicsL2 sh, Vector3 dir, Color color, float intensify)
    {

        SH9 sh9 = SH9.GetBasis(dir);


        // Normalization factor from http://www.ppsloan.org/publications/StupidSH36.pdf
        float kNormalization = Mathf.PI;//  2.9567930857315701067858823529412f; // 16*kPI/17

        Vector4 scaled = color * kNormalization * intensify;


        var km = kNormalizationConstants;

        for (int i = 0; i < 9; i++)
        {
            sh9[i] *= km[i];
            sh[0, i] += sh9[i] * scaled.x;
            sh[1, i] += sh9[i] * scaled.y;
            sh[2, i] += sh9[i] * scaled.z;

        }
    }

    //differential solid angle
    public static float AreaElement(float x, float y)
    {
        return Mathf.Atan2(x * y, Mathf.Sqrt(x * x + y * y + 1));
    }

    public static float DifferentialSolidAngle(int textureSize, float U, float V)
    {
        float inv = 1.0f / textureSize;
        float u = 2.0f * (U + 0.5f * inv) - 1;
        float v = 2.0f * (V + 0.5f * inv) - 1;
        float x0 = u - inv;
        float y0 = v - inv;
        float x1 = u + inv;
        float y1 = v + inv;
        return AreaElement(x0, y0) - AreaElement(x0, y1) - AreaElement(x1, y0) + AreaElement(x1, y1);
    }

    public static Vector3 DirectionFromCubemapTexel(int face, float u, float v)
    {
        Vector3 dir = Vector3.zero;

        switch (face)
        {
            case 0: //+X
                dir.x = 1;
                dir.y = v * -2.0f + 1.0f;
                dir.z = u * -2.0f + 1.0f;
                break;

            case 1: //-X
                dir.x = -1;
                dir.y = v * -2.0f + 1.0f;
                dir.z = u * 2.0f - 1.0f;
                break;

            case 2: //+Y
                dir.x = u * 2.0f - 1.0f;
                dir.y = 1.0f;
                dir.z = v * 2.0f - 1.0f;
                break;

            case 3: //-Y
                dir.x = u * 2.0f - 1.0f;
                dir.y = -1.0f;
                dir.z = v * -2.0f + 1.0f;
                break;

            case 4: //+Z
                dir.x = u * 2.0f - 1.0f;
                dir.y = v * -2.0f + 1.0f;
                dir.z = 1;
                break;

            case 5: //-Z
                dir.x = u * -2.0f + 1.0f;
                dir.y = v * -2.0f + 1.0f;
                dir.z = -1;
                break;
        }

        return dir.normalized;
    }

    public static int FindFace(Vector3 dir)
    {
        int f = 0;
        float max = Mathf.Abs(dir.x);
        if (Mathf.Abs(dir.y) > max)
        {
            max = Mathf.Abs(dir.y);
            f = 2;
        }
        if (Mathf.Abs(dir.z) > max)
        {
            f = 4;
        }

        switch (f)
        {
            case 0:
                if (dir.x < 0)
                    f = 1;
                break;

            case 2:
                if (dir.y < 0)
                    f = 3;
                break;

            case 4:
                if (dir.z < 0)
                    f = 5;
                break;
        }

        return f;
    }
    public static void getOffsetInCrossMap(int z,int size,out int offsetX,out int offsetY) {
        offsetX =0;
        offsetY = 0;
        if (z == 0)
        {
            offsetX = 2 * size;
            offsetY = 1*size;
        }
        if (z ==1 )
        {
            offsetX = 0 * size;
            offsetY = 1*size;
        }
        if (z == 2)
        {
            offsetX = 1 * size;
            offsetY = 2 * size;
        }
        if (z == 3)
        {
            offsetX = 1 * size;
            offsetY = 0 * size;
        }

        if (z ==4)
        {
            offsetX = 1 * size;
            offsetY = 1 * size;
        }
        if (z == 5)
        {
            offsetX = 3 * size;
            offsetY = 1 * size;
        }

 
    }
    public static int GetTexelIndexFromDirection(Vector3 dir, int cubemap_size)
    {
        float u = 0, v = 0;

        int f = FindFace(dir);

        switch (f)
        {
            case 0:
                dir.z /= dir.x;
                dir.y /= dir.x;
                u = (dir.z - 1.0f) * -0.5f;
                v = (dir.y - 1.0f) * -0.5f;
                break;

            case 1:
                dir.z /= -dir.x;
                dir.y /= -dir.x;
                u = (dir.z + 1.0f) * 0.5f;
                v = (dir.y - 1.0f) * -0.5f;
                break;

            case 2:
                dir.x /= dir.y;
                dir.z /= dir.y;
                u = (dir.x + 1.0f) * 0.5f;
                v = (dir.z + 1.0f) * 0.5f;
                break;

            case 3:
                dir.x /= -dir.y;
                dir.z /= -dir.y;
                u = (dir.x + 1.0f) * 0.5f;
                v = (dir.z - 1.0f) * -0.5f;
                break;

            case 4:
                dir.x /= dir.z;
                dir.y /= dir.z;
                u = (dir.x + 1.0f) * 0.5f;
                v = (dir.y - 1.0f) * -0.5f;
                break;

            case 5:
                dir.x /= -dir.z;
                dir.y /= -dir.z;
                u = (dir.x - 1.0f) * -0.5f;
                v = (dir.y - 1.0f) * -0.5f;
                break;
        }

        if (v == 1.0f) v = 0.999999f;
        if (u == 1.0f) u = 0.999999f;

        int index = (int)(v * cubemap_size) * cubemap_size + (int)(u * cubemap_size);

        return index;
    }

    public static bool CPU_Project_Uniform_9Coeff(Cubemap input, Vector4[] output)
    {
        for (int i = 0; i < 9; ++i)
        {
            output[i] = Vector3.zero;
        }

        if (output.Length != 9)
        {
            Debug.LogWarning("output size must be 25 for 25 coefficients");
            return false;
        }

        if (input.width != input.height)
        {
            Debug.LogWarning("input cubemap must be square");
            return false;
        }

        Color[] input_face;
        int size = input.width;

        //cycle on all 6 faces of the cubemap
        for (int face = 0; face < 6; ++face)
        {
           
            input_face = input.GetPixels((CubemapFace)face);

            //cycle all the texels
            for (int texel = 0; texel < size * size; ++texel)
            {
                float u = (texel % size) / (float)size;
                float v = ((int)(texel / size)) / (float)size;

                //get the direction vector
                Vector3 dir = DirectionFromCubemapTexel(face, u, v);
                Color radiance = input_face[texel];

                //compute the differential solid angle
                float d_omega = DifferentialSolidAngle(size, u, v);
               var sh9 = SH9.GetBasis(dir);
                //cycle for 9 coefficients
                for (int c = 0; c < 9; ++c)
                {
                     ////compute shperical harmonic
                    
                    float sh = sh9[c];
                 
                    output[c].x += radiance.r* d_omega * sh;
                    output[c].y += radiance.g * d_omega * sh;
                    output[c].z += radiance.b * d_omega * sh;
                


                }
            }
        }
 
        return true;
    }
 
     
     
}
