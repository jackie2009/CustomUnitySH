using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
[ExecuteInEditMode]
public class SphericalHarmonicsDemo : MonoBehaviour {

	public ComputeShader computeShader;
	public Cubemap skybox;
	public Cubemap irradiancemap;
	public bool clearSH; 
	public bool unityBakeSH; 
	public bool calToIrradiacnemapToSH; 
	public bool calToSH; 
	 
	void Update() {

		if (clearSH) {
			clearSH = false;
			RenderSettings.ambientProbe = new SphericalHarmonicsL2();
			
		}
		
		if (unityBakeSH) {
			unityBakeSH = false;
			RenderSettings.skybox.SetTexture("_Tex", skybox);
			Shader.SetGlobalTexture("_radiancemap", skybox);
			Lightmapping.Bake();
		}
		if (calToIrradiacnemapToSH)
		{
			calToIrradiacnemapToSH = false;
			RenderSettings.skybox.SetTexture("_Tex", skybox);
			 createIrradiacnemapAndSH();

		}	
		
		if (calToSH)
		{
			calToSH = false;
			RenderSettings.skybox.SetTexture("_Tex", skybox);
			createSH2();

		}
	}

    private void createIrradiacnemapAndSH()
    {
		var cubemapSrc = getTempCubemap();


		var renderTexture = new RenderTexture(cubemapSrc.width * 4, cubemapSrc.width * 3, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
		//renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

		renderTexture.enableRandomWrite = true;
		renderTexture.Create();

		computeShader.SetTexture(0, "Input", cubemapSrc);
		computeShader.SetInt("size", cubemapSrc.width);
		computeShader.SetTexture(0, "Result", renderTexture);
		computeShader.Dispatch(0, cubemapSrc.width / 8, cubemapSrc.height / 8, 6);




		var tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBAFloat, false, true);
		RenderTexture.active = renderTexture;
		tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);

		var path = "Assets/tempIrradiance.exr";
		File.WriteAllBytes(Application.dataPath + "/tempIrradiance.exr", tex.EncodeToEXR());
		AssetDatabase.Refresh();
		RenderTexture.active = null;
		UnityEditor.AssetDatabase.Refresh();
		var importer = AssetImporter.GetAtPath(path) as TextureImporter;
		var settings = new TextureImporterSettings();
		importer.ReadTextureSettings(settings);
		settings.textureShape = TextureImporterShape.TextureCube;
		settings.cubemapConvolution = TextureImporterCubemapConvolution.None;
		settings.readable = true;
		importer.SetTextureSettings(settings);
		importer.SaveAndReimport();

		irradiancemap= AssetDatabase.LoadAssetAtPath<Cubemap>(path);
		Shader.SetGlobalTexture("_irradiancemap", irradiancemap);


		//create sh from irrandiancemap

		var testData = new Vector4[9];
		SphericalHarmonics.CPU_Project_Uniform_9Coeff(irradiancemap, testData);
		var sh = RenderSettings.ambientProbe;
		var shKs = SH9.shArray;
		for (int i = 0; i < 9; ++i)
		{
			sh[0, i] = (shKs[i] * testData[i].x)/ Mathf.PI;
			sh[1, i] = (shKs[i] * testData[i].y)/ Mathf.PI;
			sh[2, i] = (shKs[i] * testData[i].z)/ Mathf.PI;
		}
	 
		RenderSettings.ambientProbe = sh;

	}
	Cubemap getTempCubemap() {
		var path = "Assets/tempBake.hdr";
		var probe = GetComponentInChildren<ReflectionProbe>(true);
		probe.enabled = false;


		Lightmapping.BakeReflectionProbe(probe, path);

		AssetDatabase.Refresh();
		var importer = AssetImporter.GetAtPath(path) as TextureImporter;
		var settings = new TextureImporterSettings();
		importer.ReadTextureSettings(settings);
		settings.cubemapConvolution = TextureImporterCubemapConvolution.None;
		settings.readable = true;
		importer.SetTextureSettings(settings);
		importer.SaveAndReimport();
		var radiancemap = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
		Shader.SetGlobalTexture("_radiancemap", radiancemap);
		return  radiancemap;
	}
    void createSH2()
	{


	  
		var cubemapSrc = getTempCubemap();
		var sh = new SphericalHarmonicsL2();
		int size = cubemapSrc.width;
		for (int i = 0; i < 6; i++)
		{


			var srcColors = cubemapSrc.GetPixels((CubemapFace)i);

			for (int u = 0; u < size; u++)
			{
				for (int v = 0; v < size; v++)
				{
					var dir = SphericalHarmonics.DirectionFromCubemapTexel(i, ((float)u) / size, ((float)v) / size);
 					float d_omega = SphericalHarmonics.DifferentialSolidAngle(size, u * 1.0f / size, v * 1.0f / size);
					sh.AddDirectionalLight(dir.normalized, srcColors[v * size + u], d_omega / Mathf.PI / 2);
					//SphericalHarmonics.AddDirectionalLight(ref sh, dir.normalized, srcColors[v * size + u], d_omega / Mathf.PI);
				}
			}
		}

 
		RenderSettings.ambientProbe = sh;
	}

	

 
}
