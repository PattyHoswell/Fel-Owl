using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.UI;
using UnityEngine;
using Spine;
using Spine.Unity;
using System.Reflection;

namespace Patty_FelOwl_MOD
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource LogSource { get; private set; }
        internal static Harmony PluginHarmony { get; private set; }
        internal static Sprite OwlPreviewSpr { get; private set; }

        internal static (Material mat, SpineAtlasAsset atlas, SkeletonDataAsset data) SpineWorldData;
        internal static (Material mat, SpineAtlasAsset atlas, SkeletonDataAsset data) SpineUIData;
        void Awake()
        {
            LogSource = Logger;
            try
            {
                PluginHarmony = Harmony.CreateAndPatchAll(typeof(PatchList), PluginInfo.GUID);
            }
            catch (HarmonyException ex)
            {
                LogSource.LogError((ex.InnerException ?? ex).Message);
            }

            LoadSpineAsset();

            SkeletonAnimation owl = CreateWorldOwl();
            owl.transform.localPosition = new Vector3(888888, 888888);

            Texture2D image = CaptureScreenshot(owl);
            OwlPreviewSpr = Sprite.Create(image, new Rect(0, 0, image.width, image.height), Vector2.zero);

            Destroy(owl.gameObject);
        }
        void LoadSpineAsset()
        {
            string basePath = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "Owl Spine");
            string atlasText = File.ReadAllText(Path.Combine(basePath, "export", "owl.atlas"));
            string skeletonJson = File.ReadAllText(Path.Combine(basePath, "export", "owl-pro.json"));
            var exportTextures = Directory.GetFiles(Path.Combine(basePath, "export"), "*.png");
            var imagesTextures = Directory.GetFiles(Path.Combine(basePath, "images"), "*.png");
            var allTexturePaths = exportTextures.Union(imagesTextures).ToList();

            Texture2D[] textures = new Texture2D[allTexturePaths.Count];
            for (int i = 0; i < allTexturePaths.Count; i++)
            {
                byte[] fileData = File.ReadAllBytes(allTexturePaths[i]);
                textures[i] = new Texture2D(2, 2);
                textures[i].LoadImage(fileData);
                textures[i].name = Path.GetFileNameWithoutExtension(allTexturePaths[i]);
            }
            SpineWorldData = CreateSpineAsset(atlasText, skeletonJson, textures, false);
            SpineUIData = CreateSpineAsset(atlasText, skeletonJson, textures, true);
        }

        (Material, SpineAtlasAsset, SkeletonDataAsset) CreateSpineAsset(string atlasText,
                                                                        string skeletonJson,
                                                                        Texture2D[] textures,
                                                                        bool ui
                                                                        )
        {
            Material spineMaterial;
            if (ui)
            {
                spineMaterial = new Material(Shader.Find("Spine/SkeletonGraphic"));
            }
            else
            {
                spineMaterial = new Material(Shader.Find("Spine/Skeleton"));
            }

            SpineAtlasAsset runtimeAtlasAsset = SpineAtlasAsset.CreateRuntimeInstance(new TextAsset(atlasText),
                                                                                      textures,
                                                                                      spineMaterial,
                                                                                      true
                                                                                      );

            SkeletonDataAsset runtimeSkeletonDataAsset = SkeletonDataAsset.CreateRuntimeInstance(new TextAsset(skeletonJson),
                                                                                                 runtimeAtlasAsset,
                                                                                                 true
                                                                                                 );
            return (spineMaterial, runtimeAtlasAsset, runtimeSkeletonDataAsset);
        }
        public static SkeletonAnimation CreateWorldOwl(Transform parent = null)
        {
            SkeletonAnimation instance = SkeletonAnimation.NewSkeletonAnimationGameObject(SpineWorldData.data);
            instance.name = "Owl";
            instance.gameObject.layer = LayerMask.NameToLayer("Character_Lights");
            instance.transform.SetParent(parent);
            instance.transform.localPosition = Vector3.zero;
            instance.AnimationState.SetAnimation(0, "idle", true);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public static SkeletonGraphic CreateUIOwl(Transform parent = null)
        {
            SkeletonGraphic instance = SkeletonGraphic.NewSkeletonGraphicGameObject(SpineUIData.data,
                                                                                    parent,
                                                                                    SpineUIData.mat
                                                                                    );
            instance.gameObject.layer = LayerMask.NameToLayer("UI");
            instance.name = "Owl";
            instance.transform.localPosition = Vector3.zero;

            instance.Initialize(false);
            instance.Skeleton.SetToSetupPose();
            instance.AnimationState.SetAnimation(0, "idle", true);

            instance.gameObject.SetActive(true);
            instance.transform.localScale = Vector3.one * 0.3f;
            instance.transform.localPosition = new Vector3(0, -80, 0);
            return instance;
        }

        public static Texture2D CaptureScreenshot(Component component, int width = 512, int height = 512, int padding = 10)
        {
            Renderer renderer = component.GetComponent<Renderer>();
            if (renderer == null)
            {
                LogSource.LogError("Cannot obtain renderer to take screenshot");
                return null;
            }

            GameObject cameraGO = new GameObject("ScreenshotCamera");
            Camera cam = cameraGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;

            RenderTexture rt = new RenderTexture(width, height, 32, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            Bounds bounds = renderer != null ? renderer.bounds : new Bounds(Vector3.zero, new Vector3(2, 2, 0));

            float aspect = width / (float)height;
            float orthoSize = Mathf.Max(bounds.extents.y, bounds.extents.x / aspect) * (1 + padding / 100f);
            cam.orthographicSize = orthoSize;

            Vector3 center = bounds.center;
            cam.transform.position = new Vector3(center.x,
                                                 center.y,
                                                 center.z - 10f);

            cam.Render();

            Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var oldRT = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            
            RenderTexture.active = oldRT;
            Destroy(cameraGO);
            Destroy(rt);

            return tex;
        }
    }
}
