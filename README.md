# AssetBundleLoadTest

アセットバンドル読み込み速度実験プロジェクト。<br />
アセットバンドルのビルドオプション＋読みこみ方式の種類と様々あるので、その実験

## ビルド
・Tools/BuildBundlesでアセットバンドルのビルドをします。

## テスト
###TestPrefab
AssetBundleからGameObjectを取り出すのにかかった時間を図ります。<br />
AssetBundle自体のロード、Objectのロードの二つに分けて計測します。

###TestScenes
AssetBundleにあるシーンのロード時間を図ります。<br />
AssetBundle自体のロード、シーンのロードの二つに分けて計測します。

