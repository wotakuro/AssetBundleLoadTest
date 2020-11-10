using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class AssetBundleLoader
{
    public AssetBundle lastAssetBundle { get; private set; }
    public GameObject lastAsyncLoadPrefab { get; private set; }

    private Dictionary<string, uint> abCRC;

    public AssetBundleLoader(Dictionary<string,uint> assetBundleCrc)
    {
        this.abCRC = assetBundleCrc;
    }

    public IEnumerator LoadFromFsSync(string file)
    {
        this.lastAssetBundle = AssetBundle.LoadFromStream(File.OpenRead(file));
        yield break;
    }
    public IEnumerator LoadFromFsSyncWithCRC(string file)
    {
        uint crc = 0;
        if (this.abCRC.TryGetValue(file, out crc))
        {
            this.lastAssetBundle = AssetBundle.LoadFromStream(File.OpenRead(file),crc);
        }
        else
        {
        }
        yield break;
    }
    public IEnumerator LoadFromBufferedFsSync(string file)
    {
        this.lastAssetBundle = AssetBundle.LoadFromStream( new BufferedStream( File.OpenRead(file) ));
        yield break;
    }

    public IEnumerator LoadFromFileAsync(string file)
    {
        var req = AssetBundle.LoadFromFileAsync(file);
        while (!req.isDone)
        {
            yield return null;
        }

        this.lastAssetBundle = req.assetBundle;
        yield break;
    }


    public IEnumerator LoadFromFileSync(string file)
    {
        this.lastAssetBundle = AssetBundle.LoadFromFile(file);
        yield break;
    }

    public IEnumerator LoadFromFileSyncWithCRC(string file)
    {
        uint crc = 0;
        if( this.abCRC.TryGetValue(file, out crc)){
            this.lastAssetBundle = AssetBundle.LoadFromFile(file,crc);
        }
        else
        {
            Debug.LogError("No CRC " + file);
        }
        yield break;
    }


    public IEnumerator LoadFromMemorySync(string file)
    {
        var bin = File.ReadAllBytes(file);
        this.lastAssetBundle = AssetBundle.LoadFromMemory(bin);
        yield break;
    }
    public IEnumerator LoadFromMemorySyncWithCRC(string file)
    {

        uint crc = 0;
        if (this.abCRC.TryGetValue(file, out crc))
        {
            var bin = File.ReadAllBytes(file);
            this.lastAssetBundle = AssetBundle.LoadFromMemory(bin,crc);
        }
        else
        {
            Debug.LogError("No CRC " + file);
        }
        yield break;
    }

    public IEnumerator LoadPrefabFromAbSync(AssetBundle ab)
    {
        var prefab = ab.LoadAllAssets<GameObject>();
        if(prefab == null || prefab.Length == 0) {
            lastAsyncLoadPrefab = null;
            yield break;
        }
        lastAsyncLoadPrefab = prefab[0];
        yield break;
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
