using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CopyFileForAndroid 
{
    public IEnumerator CopyFiles()
    {
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
        }
    }

    private IEnumerator CopyFile(string file)
    {
        WWW www = new WWW(Path.Combine(Application.streamingAssetsPath, file));
        while (!www.isDone)
        {
            yield return null;
        }
        File.WriteAllBytes( Path.Combine(Application.persistentDataPath,file),www.bytes);
    }

    private List<string > GetFileList(string listText)
    {
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
