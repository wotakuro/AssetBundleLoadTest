using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class AssetBundleLoader
{
    public AssetBundle lastAssetBundle { get; private set; }
    public GameObject lastAsyncLoadPrefab { get; private set; }



    public GameObject LoadPrefabFromAbSync(AssetBundle ab)
    {
        var prefab = ab.LoadAllAssets<GameObject>();
        if(prefab == null || prefab.Length == 0) { return null; }
        return prefab[0];
    }


    public IEnumerator LoadPrefabFromAbAsync(AssetBundle ab)
    {
        var request = ab.LoadAllAssetsAsync<GameObject>();
        while (!request.isDone)
        {
            yield return null;
        }
        lastAsyncLoadPrefab = request.asset as GameObject;
    }

    public static uint GetCRCFromManifest(string file)
    {
        if( !File.Exists(file))
        {
            return 0;
        }
        using (var fs = File.OpenRead(file))
        {
            using(var stream = new StreamReader(fs))
            {
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();
                    if(line.StartsWith("CRC:"))
                    {
                        var crcStr = line.Substring(4).Trim();
                        return uint.Parse(crcStr);
                    }
                }
            }
        }
        return 0;
    }
}
