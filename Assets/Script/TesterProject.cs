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
        yield return null;
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
        
        ie = SyncLoadTestWithoutCRC();
        while (ie.MoveNext()) { yield return null; }
        ie = SyncLoadTestWithCRC();
        while (ie.MoveNext()) { yield return null; }



        this.ApplyResultInfos();
    }

    private IEnumerator SyncLoadTestWithCRC()
    {
        AssetBundleLoader loader = new AssetBundleLoader();
        for (int i = 0; i < this.assetBundleFiles.Count; ++i)
        {
            var file = this.assetBundleFiles[i];
            uint crc = 0;
            if(!this.abCRC.TryGetValue(file,out crc)){
                crc = 0;
            }
            ResultInfo info = new ResultInfo(this.assetBundleFileNameOnly[i], "LoadSyncWithCRC", Time.realtimeSinceStartup);
            var ab = AssetBundle.LoadFromFile(file,crc);
            GameObject prefab = null;
            info.endTime = Time.realtimeSinceStartup;
            {
                info.startLoadPrefab = info.endTime;
                prefab = loader.LoadPrefabFromAbSync(ab);
                info.endLoadPrefab = Time.realtimeSinceStartup;
            }
            ab.Unload(true);

            this.resultInfos.Add(info);
        }
        this.ApplyResultInfos();
        yield break;
    }

    private IEnumerator SyncLoadTestWithoutCRC() {
        AssetBundleLoader loader = new AssetBundleLoader();
        for (int i=0;i< this.assetBundleFiles.Count;++i)
        {
            var file = this.assetBundleFiles[i];
            ResultInfo info = new ResultInfo(this.assetBundleFileNameOnly[i],"LoadFileSync",Time.realtimeSinceStartup);
            var ab = AssetBundle.LoadFromFile(file);
            GameObject prefab = null;
            info.endTime = Time.realtimeSinceStartup;
            {
                info.startLoadPrefab = info.endTime;
                prefab = loader.LoadPrefabFromAbSync(ab);
                info.endLoadPrefab = Time.realtimeSinceStartup;
            }
            ab.Unload(true);

            this.resultInfos.Add(info);
        }
        yield break;
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
                sb.Append(result.fileName).Append("\n");
                lastFile = result.fileName;
            }

            sb.Append("  ").Append(result.methodName).Append(" ").
                Append(time).Append("ms ").Append(prefabTime).Append("ms");

            sb.Append("\n");
        }
        this.resultTxt.text += sb.ToString();
        resultInfos.Clear();
    }
}
