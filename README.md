# Cluster World Tools
## 概要
CCKでのワールド制作を便利にするエディタ拡張です。

## 導入方法
ReleasesからLatestを選択し、ClusterWorldTools.unitypackageをダウンロードしてください。

ダウンロードしたものをClusterワールドのUnityプロジェクトにインポートしてください。

## 使い方（メニュー機能）
上部メニュー「WorldTools」から利用できる機能です。

![image1](https://user-images.githubusercontent.com/64951202/198227274-55f86bb4-2170-4bcb-9c37-91ba1141be7c.png)

### Webトリガー生成（かんたん）
シーン内のギミックコンポーネントからbool型とsignal型のKeyの一覧を表示し、選択してWebトリガー用のJSONファイルを書き出すことができます。

書き出したJSONファイルはイベントページやエディタ上のWebトリガーウインドウから読み込んで使うことができます。

イベントでの利用のほか、デバッグ時に簡易的にギミックを動かす場合などにもオススメです。

![image5](https://user-images.githubusercontent.com/64951202/198228837-4032f514-32d8-43db-97ac-a90919f59c4b.png)

### Webトリガー生成（詳細）
詳細な設定をしてWebトリガーを生成しJSONに書き出すことができます。

手入力のほか、ギミックコンポーネントのついたオブジェクトを読み込むことでいくつかの項目を自動入力できます。
また、既存のJSONを読み込むこともできます。

こちらは設定できる項目が多く、複雑なWebトリガーを作成できます。
Webトリガーの詳細は[Cluster Creator Kit公式ドキュメント](https://docs.cluster.mu/creatorkit/event/web-trigger/)を参照してください。

![image3](https://user-images.githubusercontent.com/64951202/198228933-dd8edb9d-dcd0-46f4-99b7-a7fab1e966da.png)

### レイヤー自動設定
clusterのワールドに利用できるレイヤーを自動で設定します。

### トリガーモニター
プレビュー実行中にトリガーの値を確認できます。Key検索やアイテム検索で抽出して表示することもできます。

Integer型やFloat型は値がそのまま、Bool型は0か1で表示されます。
Signal型は変更されたときに緑色に光ります。

![image6](https://user-images.githubusercontent.com/64951202/198227542-93e42af8-5088-46f2-b512-4c428e72c7c0.png)

### テクスチャインポート設定
テクスチャインポート時にサイズ上限とモバイルでの圧縮形式を自動で設定し、一括で軽量化することができます。
PC・モバイル別のサイズ上限と、モバイルでの圧縮品質を選択できます。

初期状態ではこの機能は無効です。設定ウインドウで「制限を適用」にチェックを入れ、再インポートをおこなうと適用されます。

※この設定は全てのプロジェクトで共有されます。有効になっていると、他のプロジェクトに導入したときは始めから有効になります。

![image4](https://user-images.githubusercontent.com/64951202/198227683-9f7a608f-927e-493c-b615-7770a69e3ad0.png)

### 改善チェック
よくある不具合・エラーの原因をチェックしてConsoleに表示します。 一部は自動修正することができます。

Console上のエラーや警告をクリックすると、不備のあるオブジェクトを確認することができます。

![image7](https://user-images.githubusercontent.com/64951202/198227974-67c0aa96-07c9-4d53-91cf-c95f571f2c25.png)

## 使い方（その他の機能）

### 基本オブジェクト作成
GameObjectメニューからSpawnPoint, DespawnHeight, MainScreenを作成できます。

### Audio Listener自動削除
Camera設置時などに追加されるAudio Listenerコンポーネントを自動で削除します。
