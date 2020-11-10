﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssetBundleBuilder
{

    private static List<string> headers;
    [MenuItem("Tools/BuildBundles")]
    public static void Execute()
    {
        headers = new List<string>();
        BuildBundle("uncompress", BuildAssetBundleOptions.UncompressedAssetBundle);
        BuildBundle("chunkbase", BuildAssetBundleOptions.ChunkBasedCompression);
        BuildBundle("nooption", BuildAssetBundleOptions.None);
        BuildBundle("disableFilename", BuildAssetBundleOptions.DisableLoadAssetByFileName);
        BuildBundle("disableFilenameWithExt", BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension);
        BuildBundle("disableTree", BuildAssetBundleOptions.DisableWriteTypeTree);
        CreateAssetBudleFileList();
        CreateCopyListText();
    }

    private static void CreateAssetBudleFileList()
    {
        var sb = new System.Text.StringBuilder(512);
        foreach (var header in headers)
        {
            sb.Append(header).Append("_unitychan.bundle").Append("\n");
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(Application.streamingAssetsPath, "ab_list.txt"), sb.ToString());

    }

    private  static void CreateCopyListText()
    {
        var sb = new System.Text.StringBuilder(512);
        foreach ( var header in headers)
        {
            sb.Append(header).Append("_unitychan.bundle").Append("\n");
            sb.Append(header).Append("_unitychan.bundle.manifest").Append("\n");
        }
        sb.Append("ab_list.txt");
        System.IO.File.WriteAllText(System.IO.Path.Combine(Application.streamingAssetsPath, "list.txt"), sb.ToString());
    }

    private static void BuildBundle(string headerName,BuildAssetBundleOptions option)
    {
        headers.Add(headerName);
        SetupAssetBundleName(headerName);
        BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, option, EditorUserBuildSettings.activeBuildTarget);
    }
    private static void SetupAssetBundleName(string headerName)
    {
        string path = "Assets/UnityChan/SD_unitychan/Prefabs/SD_unitychan_generic.prefab";
        AssetImporter importer = AssetImporter.GetAtPath(path);
        importer.assetBundleName = headerName + "_unitychan.bundle";



    }
}