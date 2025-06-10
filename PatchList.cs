using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ShinyShoe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Spine.Unity;
using System.Reflection;
using UnityEngine.UI;
using System.IO;
using BepInEx;
using System.Collections;

namespace Patty_FelOwl_MOD
{
    internal class PatchList
    {
        [HarmonyPostfix, HarmonyPatch(typeof(ShinyShoe.AppManager), "DoesThisBuildReportErrors")]
        public static void DisableErrorReportingPatch(ref bool __result)
        {
            __result = false;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(CharacterUIMeshSpine), "PlayAnim", new Type[]
        {
            typeof(int),
            typeof(CharacterUI.Anim),
            typeof(bool),
            typeof(float),
            typeof(float),
            typeof(Action<CharacterUI.AnimNote>),
        })]
        public static void PlayAnim(CharacterUIMeshSpine __instance, CharacterUI.Anim animType, bool loop, float startTime)
        {
            if (__instance.SkeletonAnimation.name == "Owl")
            {
                // Disable Fel
                __instance.transform.GetChild(0).gameObject.SetActive(false);

                __instance.SkeletonAnimation.gameObject.SetActive(true);
                switch (animType)
                {
                    case CharacterUI.Anim.Attack:
                    case CharacterUI.Anim.Attack_Spell:
                        __instance.SkeletonAnimation.AnimationState.SetAnimation(0, "left", loop).TrackTime = startTime;
                        break;
                    case CharacterUI.Anim.Death:
                    case CharacterUI.Anim.HitReact:
                        __instance.SkeletonAnimation.AnimationState.SetAnimation(0, "right", loop).TrackTime = startTime;
                        break;
                    default:
                        __instance.SkeletonAnimation.AnimationState.SetAnimation(0, "idle", true).TrackTime = startTime;
                        break;
                }
                var characterUI = __instance.transform.parent;

                // Disable Fel effect
                characterUI.Find("VfxAnchors").gameObject.SetActive(false);
                characterUI.Find("FX_NewSpeedLines").gameObject.SetActive(false);
                characterUI.Find("HandFX").gameObject.SetActive(false);
                characterUI.Find("HeadFX").gameObject.SetActive(false);
            }
        }
        [HarmonyPrefix, HarmonyPatch(typeof(AnimateCardEffects), "Initialize")]
        public static bool Initialize(AnimateCardEffects __instance)
        {
            var image = __instance.GetComponent<Image>();
            if (image != null &&
                image.sprite != null &&
                image.sprite.name == "PRT_Fel_Base")
            {
                if (image.transform.Find("Owl") == null)
                {
                    Plugin.CreateUIOwl(__instance.transform);
                }
                return false;
            }
            return true;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(CharacterDialogueScreen), "InstantiatePrefab")]
        public static void InstantiatePrefab(ref GameObject __result)
        {
            if (__result != null)
            {
                var image = __result.GetComponentInChildren<Image>();
                if (image != null &&
                    image.sprite != null)
                {
                    if (image.sprite.name.IndexOf("Dialog_Fel", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Transform tr = image.transform;
                        UnityEngine.Object.DestroyImmediate(image);
                        tr.localPosition += new Vector3(170f, 0);
                        Plugin.CreateUIOwl(tr).transform.localScale = Vector3.one * 0.8f;
                    }
                }
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(CharacterUIMeshSpine), "Setup")]
        public static void Setup(CharacterUIMeshSpine __instance, string debugName, ref List<CharacterUIMeshSpine.ModelEntry> ___modelVariations)
        {
            if (debugName.IndexOf("Character_Fel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var owl = Plugin.CreateWorldOwl(__instance.transform);
                owl.transform.localScale = Vector3.one * 0.5f;
                owl.transform.localPosition = new Vector3(0, -2.19f, 0);
                foreach (var model in ___modelVariations)
                {
                    if (model == null)
                    {
                        continue;
                    }
                    for (var i = 0; i < model.animInfos.Length; i++)
                    {
                        if (model.animInfos[i] == null)
                        {
                            continue;
                        }
                        model.animInfos[i] = new CharacterUIMeshSpine.AnimInfo(CharacterUI.Anim.Idle,
                                                                               owl,
                                                                               owl.GetComponent<MeshRenderer>(),
                                                                               model.animInfos[i].animation);
                        __instance.PlayAnimLoop(CharacterUI.Anim.Idle, 0f);
                    }
                }
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(RoomTargetingUI), "CharacterPlacementPreview", new Type[]
        {
            typeof(SpawnPoint),
            typeof(CharacterData),
            typeof(MonsterManager),
        })]
        public static void CharacterPlacementPreview(RoomTargetingUI __instance, ref SpriteRenderer ___characterPreview)
        {
            if (___characterPreview != null &&
                ___characterPreview.sprite != null &&
                ___characterPreview.sprite.name == "PLR_Fel")
            {
                ___characterPreview.sprite = Plugin.OwlPreviewSpr;
                ___characterPreview.transform.localPosition += new Vector3(-2.12f, -3.45f);
            }
        }
    }
}
