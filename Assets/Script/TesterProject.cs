using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TesterProject : MonoBehaviour
{
    public Button testBtn;
    public Text resultTxt;

    private string basePath;
    private IEnumerator executeEnumerator;

    private List<string> assetBundleFiles;
    private List<string> assetBundleFileNameOnly;
    private Dictionary<string, uint> abCRC;

    private struct ResultInfo
    {
        public string fileName;
        public string methodName;
        public float startTime;
        public float endTime;

        public float startLoadPrefab;
        public float endLoadPrefab;

        public ResultInfo( string file , string method,float stTime)
        {
            this.fileName = file;
            this.methodName = method;
            this.startTime = stTime;
            this.endTime = 0.0f;

            this.startLoadPrefab = 0.0f;
            this.endLoadPrefab = 0.0f;
        }
    }

    List<ResultInfo> resultInfos;


    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 240;
        testBtn.gameObject.SetActive(false);
#if !UNITY_EDITOR && UNITY_ANDROID
        basePath = Application.persistentDataPath;
#else
        basePath = Application.streamingAssetsPath;
#endif
    }

    private void InitInfo()
    {
        assetBundleFiles = new List<string>();
        assetBundleFileNameOnly = new List<string>();
        abCRC = new Dictionary<string, uint>();

        var lines = File.ReadAllLines(Path.Combine(basePath, "ab_list.txt"));
        foreach( var line in lines)
        {
            string file = line.Trim();
            if (string.IsNullOrEmpty(file)) { continue; }
            assetBundleFileNameOnly.Add(file);
            string abPath = Path.Combine(basePath, file);
            assetBundleFiles.Add(abPath);
            uint crc = AssetBundleLoader.GetCRCFromManifest(abPath + ".manifest");
            this.abCRC.Add(abPath, crc);
        }

        this.resultInfos = new List<ResultInfo>(lines.Length * 8);
    }

    IEnumerator Start()
    {
        var copyObj = new CopyFileForAndroid();
        var ie = copyObj.CopyFiles();
        while (ie.MoveNext())
        {
            yield return null;
        }
        testBtn.gameObject.SetActive(true);
        testBtn.onClick.AddListener(ExecuteTest);
        this.InitInfo();
    }

    public void Update()
    {
        if (executeEnumerator != null)
        {
            if(!executeEnumerator.MoveNext() ){
                executeEnumerator = null;
            }
        }
    }

    private void ExecuteTest()
    {
        if(executeEnumerator != null) { return; }
        executeEnumerator = Execute();
    }

    private IEnumerator Execute()
    {
        IEnumerator ie = null;
        var loader = new AssetBundleLoader( this.abCRC);
        // Sync
        ie = TestAssetBundleFiles("LoadSync", loader, loader.LoadFromFileSync, loader.LoadPrefabFromAbSync);
        while (ie.MoveNext()) { yield return null; }
        ie = TestAssetBundleFiles("LoadSyncCRC", loader, loader.LoadFromFileSyncWithCRC, loader.LoadPrefabFromAbSync);
        while (ie.MoveNext()) { yield return null; }
        ie = TestAssetBundleFiles("MemorySync", loader, loader.LoadFromMemorySync, loader.LoadPrefabFromAbSync);
        while (ie.MoveNext()) { yield return null; }
        ie = TestAssetBundleFiles("MemorySyncWithCRC", loader, loader.LoadFromMemorySyncWithCRC, loader.LoadPrefabFromAbSync);
        while (ie.MoveNext()) { yield return null; }
        ie = TestAssetBundleFiles("StreamSync", loader, loader.LoadFromFsSync, loader.LoadPrefabFromAbSync);
        while (ie.MoveNext()) { yield return null; }
        ie = TestAssetBundleFiles("StreamSyncWithCRC", loader, loader.LoadFromFsSyncWithCRC, loader.LoadPrefabFromAbSync);
        while (ie.MoveNext()) { yield return null; }
        ie = TestAssetBundleFiles("BufferedFsSync", loader, loader.LoadFromBufferedFsSync, loader.LoadPrefabFromAbSync);
        while (ie.MoveNext()) { yield return null; }

        // Async
        ie = TestAssetBundleFiles("LoadAsync", loader, loader.LoadFromFileAsync, loader.LoadPrefabFromAbAsync);
        while (ie.MoveNext()) { yield return null; }

        this.ApplyResultInfos();
    }

    private IEnumerator TestAssetBundleFiles(string name,
        AssetBundleLoader loader,
        System.Func<string,IEnumerator> loadAb,
        System.Func<AssetBundle, IEnumerator> loadPrefab)
    {
        IEnumerator ie = null;
        for (int i = 0; i < this.assetBundleFiles.Count; ++i)
        {
            var file = this.assetBundleFiles[i];
            ResultInfo info = new ResultInfo(this.assetBundleFileNameOnly[i], name, Time.realtimeSinceStartup);

            ie = loadAb(file);
            while (ie.MoveNext())
            {
                yield return null;
            }
            var ab = loader.lastAssetBundle;

            info.endTime = Time.realtimeSinceStartup;
            {
                info.startLoadPrefab = info.endTime;
                ie = loadPrefab(ab);
                while (ie.MoveNext())
                {
                    yield return null;
                }
                info.endLoadPrefab = Time.realtimeSinceStartup;
            }
            ab.Unload(true);

            this.resultInfos.Add(info);
        }
        yield return null;
        yield return null;
    }

    public void ClearText()
    {
        this.resultTxt.text = "";
    }


    private void ApplyResultInfos()
    {
        var sb = new System.Text.StringBuilder(1024);
        this.resultInfos.Sort((a, b) =>
        {
            int file= a.fileName.CompareTo(b.fileName);
            if(file != 0) {
                return file;
            }
            return a.methodName.CompareTo(b.methodName);
        });
        string lastFile = null;

        foreach( var result in this.resultInfos)
        {
            float time = (result.endTime - result.startTime) * 1000.0f;
            float prefabTime = (result.endLoadPrefab - result.startLoadPrefab)*1000.0f;
            if( lastFile != result.fileName)
            {
                sb.Append("-").Append(result.fileName).Append("\n");
                lastFile = result.fileName;
            }

            sb.Append("   ").Append(result.methodName).Append(" ").
                Append(time).Append("ms ").Append(prefabTime).Append("ms");

            sb.Append("\n");
        }
        this.resultTxt.text += sb.ToString();

        Debug.Log(sb.ToString());
        resultInfos.Clear();
    }
}
