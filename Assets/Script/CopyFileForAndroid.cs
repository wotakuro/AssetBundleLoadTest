using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CopyFileForAndroid 
{
    public IEnumerator CopyFiles()
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        WWW www = new WWW(Path.Combine(Application.streamingAssetsPath, "list.txt"));
        while (!www.isDone) { yield return null; }
        var list = GetFileList(www.text);
        yield return null;
        foreach(var file in list)
        {
            var ienumerator = CopyFile(file);
            while (ienumerator.MoveNext()){
                yield return null;
            }
            yield return null;
        }
#else
        yield break;
#endif
    }

    private IEnumerator CopyFile(string file)
    {
        WWW www = new WWW(Path.Combine(Application.streamingAssetsPath, file));
        while (!www.isDone)
        {
            yield return null;
        }
        Debug.Log("CopyFile " + file + "::" + www.bytes.Length);
        File.WriteAllBytes( Path.Combine(Application.persistentDataPath,file),www.bytes);
        www.Dispose();
    }

    private List<string > GetFileList(string listText)
    {
        Debug.Log(listText);
        var list = new List<string>();
        var tmpArr = listText.Split('\n');
        foreach( var tmpTxt in tmpArr)
        {
            var txt = tmpTxt.Trim();
            if(string.IsNullOrEmpty(txt)) { continue; }
            list.Add(txt);
        }

        return list;
    }
}
