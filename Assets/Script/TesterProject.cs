using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TesterProject : MonoBehaviour
{
    public Button testBtn;
    public Button sceneBtn;
    public Text resultTxt;

    private string basePath;
    private IEnumerator prefabExecIe;
    private IEnumerator sceneExecIe;

    private List<string> assetBundleFiles;
    private List<string> assetBundleFileNameOnly;


    private List<string> sceneBundleFiles;
    private List<string> sceneBundleFileNameOnly;
    private Dictionary<string, uint> abCRC;

    private struct ResultInfo
    {
        public string fileName;
        public string methodName;
        public float startTime;
        public float endTime;

        public float startLoadObject;
        public float endLoadObject;

        public ResultInfo( string file , string method,float stTime)
        {
            this.fileName = file;
            this.methodName = method;
            this.startTime = stTime;
            this.endTime = 0.0f;

            this.startLoadObject = 0.0f;
            this.endLoadObject = 0.0f;
        }
    }

    List<ResultInfo> resultInfos;


    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 240;
        testBtn.gameObject.SetActive(false);
        sceneBtn.gameObject.SetActive(false);
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

        sceneBundleFiles = new List<string>();
        sceneBundleFileNameOnly = new List<string>();
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
        lines = File.ReadAllLines(Path.Combine(basePath, "ab_scenes.txt"));
        foreach (var line in lines)
        {
            string file = line.Trim();
            if (string.IsNullOrEmpty(file)) { continue; }
            sceneBundleFileNameOnly.Add(file);
            string abPath = Path.Combine(basePath, file);
            sceneBundleFiles.Add(abPath);
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
        testBtn.onClick.AddListener(PrefabTestStart);
        sceneBtn.gameObject.SetActive(true);
        sceneBtn.onClick.AddListener(ScenesTestStart);
        this.InitInfo();
    }

    public void Update()
    {
        if (prefabExecIe != null)
        {
            if (!prefabExecIe.MoveNext())
            {
                prefabExecIe = null;
            }
        }
        if (sceneExecIe != null)
        {
            if (!sceneExecIe.MoveNext())
            {
                sceneExecIe = null;
            }
        }
    }

    private void ScenesTestStart()
    {
        if(this.sceneExecIe != null) { return; }
        this.sceneExecIe = ExecuteSceneTest();
    }

    private void PrefabTestStart()
    {
        if(prefabExecIe != null) { return; }
        prefabExecIe = ExecutePrefabTest();
    }

    private IEnumerator ExecutePrefabTest()
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


    private IEnumerator ExecuteSceneTest()
    {
        IEnumerator ie = null;
        var loader = new AssetBundleLoader(this.abCRC);

        ie = TestSceneBundleFiles("SceneSync", loader, loader.LoadFromFileSync, loader.LoadScenesSync);
        while (ie.MoveNext()) { yield return null; }
        ie = TestSceneBundleFiles("SceneSyncWithCRC", loader, loader.LoadFromFileSyncWithCRC, loader.LoadScenesSync);
        while (ie.MoveNext()) { yield return null; }
        ie = TestSceneBundleFiles("SceneStreamSync", loader, loader.LoadFromFsSync, loader.LoadScenesSync);
        while (ie.MoveNext()) { yield return null; }
        ie = TestSceneBundleFiles("SceneStreamSyncWithCRC", loader, loader.LoadFromFsSyncWithCRC, loader.LoadScenesSync);
        while (ie.MoveNext()) { yield return null; }
        //Async
        ie = TestSceneBundleFiles("SceneSync", loader, loader.LoadFromFileAsync, loader.LoadScenesAsync);
        while (ie.MoveNext()) { yield return null; }

        this.ApplyResultInfos();
        yield break;
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
                info.startLoadObject = info.endTime;
                ie = loadPrefab(ab);
                while (ie.MoveNext())
                {
                    yield return null;
                }
                info.endLoadObject = Time.realtimeSinceStartup;
            }
            ab.Unload(true);

            this.resultInfos.Add(info);
        }
        yield return null;
        yield return null;
    }


    private IEnumerator TestSceneBundleFiles(string name,
        AssetBundleLoader loader,
        System.Func<string, IEnumerator> loadAb,
        System.Func<AssetBundle, IEnumerator> loadScene)
    {
        IEnumerator ie = null;
        for (int i = 0; i < this.sceneBundleFiles.Count; ++i)
        {
            var file = this.sceneBundleFiles[i];
            ResultInfo info = new ResultInfo(this.sceneBundleFileNameOnly[i], name, Time.realtimeSinceStartup);

            ie = loadAb(file);
            while (ie.MoveNext())
            {
                yield return null;
            }
            var ab = loader.lastAssetBundle;

            info.endTime = Time.realtimeSinceStartup;
            {
                info.startLoadObject = info.endTime;
                ie = loadScene(ab);
                while (ie.MoveNext())
                {
                    yield return null;
                }
                info.endLoadObject = Time.realtimeSinceStartup;
            }
            for (int j = 0; j < 10; ++j)
            {
                yield return null;
            }

            ie = loader.UnloadLastScenes();
            while (ie.MoveNext())
            {
                yield return null;
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
            float prefabTime = (result.endLoadObject - result.startLoadObject)*1000.0f;
            if( lastFile != result.fileName)
            {
                sb.Append("-- ").Append(result.fileName).Append("\n");
                lastFile = result.fileName;
            }

            sb.Append("   ").Append(result.methodName).Append(" ").
                Append(time).Append("ms ").Append(prefabTime).Append("ms");

            sb.Append("\n");
        }
        this.resultTxt.text += sb.ToString();

        Log(sb.ToString());
        resultInfos.Clear();
    }

    private void Log(string str)
    {
        int current = 0;

        while (current < str.Length)
        {
            int next = str.IndexOf("-- ",current);
            if( next == -1)
            {
                next = str.Length;
            }
            int length = next - current;
            Debug.Log(str.Substring(current,length) );
            current = next + 3;
        }
    }
}
