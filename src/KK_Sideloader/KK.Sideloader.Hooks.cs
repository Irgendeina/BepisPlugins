﻿using HarmonyLib;
using Sideloader.ListLoader;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sideloader
{
    public partial class Sideloader
    {
        internal static partial class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(Studio.Info), "LoadExcelData")]
            internal static void LoadExcelDataPostfix(string _bundlePath, string _fileName, ref ExcelData __result)
            {
                var studioList = Lists.ExternalStudioDataList.Where(x => x.AssetBundleName == _bundlePath && x.FileNameWithoutExtension == _fileName).ToList();

                if (studioList.Count > 0)
                {
                    bool didHeader = false;
                    int HeaderRows = studioList[0].Headers.Count;

                    if (__result == null) //Create a new ExcelData
                        __result = (ExcelData)ScriptableObject.CreateInstance(typeof(ExcelData));
                    else //Adding to an existing ExcelData
                        didHeader = true;

                    foreach (var studioListData in studioList)
                    {
                        if (!didHeader) //Write the headers. I think it's pointless and will be skipped when the ExcelData is read, but it's expected to be there.
                        {
                            foreach (var header in studioListData.Headers)
                            {
                                var headerParam = new ExcelData.Param();
                                headerParam.list = header;
                                __result.list.Add(headerParam);
                            }
                            didHeader = true;
                        }
                        foreach (var entry in studioListData.Entries)
                        {
                            var param = new ExcelData.Param();
                            param.list = entry;
                            __result.list.Add(param);
                        }
                    }

                    //Once the game code hits a blank row it skips everything after, all blank rows must be removed for sideloader data to display.
                    for (int i = 0; i < __result.list.Count;)
                    {
                        if (i <= HeaderRows - 1)
                            i += 1; //Skip header rows
                        else if (__result.list[i].list.Count == 0)
                            __result.list.RemoveAt(i); //Null data row
                        else if (!int.TryParse(__result.list[i].list[0], out int x))
                            __result.list.RemoveAt(i); //Remove anything that isn't a number, most likely a blank row
                        else
                            i += 1;
                    }
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(BaseMap), "LoadMapInfo")]
            internal static void LoadMapInfo(BaseMap __instance)
            {
                foreach (var mapInfo in Lists.ExternalMapList)
                    foreach (var param in mapInfo.param)
                        __instance.infoDic[param.No] = param;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(Studio.AssetBundleCheck), nameof(Studio.AssetBundleCheck.GetAllFileName))]
            internal static bool GetAllFileName(string _assetBundleName, ref string[] __result)
            {
                var list = Lists.ExternalStudioDataList.Where(x => x.AssetBundleName == _assetBundleName).Select(y => y.FileNameWithoutExtension.ToLower()).ToArray();
                if (list.Count() > 0)
                {
                    __result = list;
                    return false;
                }
                return true;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(Studio.Info), "FindAllAssetName")]
            internal static bool FindAllAssetNamePrefix(string _bundlePath, string _regex, ref string[] __result)
            {
                var list = Lists.ExternalStudioDataList.Where(x => x.AssetBundleName == _bundlePath).Select(x => x.FileNameWithoutExtension).ToList();
                if (list.Count() > 0)
                {
                    __result = list.Where(x => Regex.Match(x, _regex, RegexOptions.IgnoreCase).Success).ToArray();
                    return false;
                }
                return true;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(CommonLib), nameof(CommonLib.GetAssetBundleNameListFromPath))]
            internal static void GetAssetBundleNameListFromPath(string path, List<string> __result)
            {
                if (path == "studio/info/")
                {
                    foreach (string assetBundleName in Lists.ExternalStudioDataList.Select(x => x.AssetBundleName).Distinct())
                        if (!__result.Contains(assetBundleName))
                            __result.Add(assetBundleName);
                }
            }
            /// <summary>
            /// Patch for loading h/common/ stuff for Sideloader maps
            /// </summary>
            internal static void LoadAllFolderPostfix(string _findFolder, string _strLoadFile, ref List<Object> __result)
            {
                if (__result.Count() == 0 && _findFolder == "h/common/")
                    foreach (var kvp in BundleManager.Bundles.Where(x => x.Key.StartsWith(_findFolder)))
                        foreach (var lazyList in kvp.Value)
                            foreach (var assetName in lazyList.Instance.GetAllAssetNames())
                                if (assetName.ToLower().Contains(_strLoadFile.ToLower()))
                                {
                                    GameObject go = CommonLib.LoadAsset<GameObject>(kvp.Key, assetName);
                                    if (go)
                                        __result.Add(go);
                                }
            }
        }
    }
}