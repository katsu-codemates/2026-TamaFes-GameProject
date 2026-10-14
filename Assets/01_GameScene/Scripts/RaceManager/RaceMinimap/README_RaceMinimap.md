# レースミニマップ(進行バー) セットアップ手順

`RaceMinimap` / `RaceMinimapIcon` はスクリプトのみで、UIはGameSceneにエディタで配置する。
コースは直線なので、横長のバーの左端=スタート、右端=ゴールとして、各走者のアイコンが `progress`(0~1) に合わせて進む。

## 1. ミニマップ本体
1. `UICanvas` の下に空の UI オブジェクト `RaceMinimap` を作る。
   - 画面上端に横長で置く例: Anchor `top-stretch`、Height 90〜120、左右の余白 40 程度。
   - 実況テキスト(`CommentBg`)や出走表・結果画面と重ならない位置にする。
2. 背景用の `Image`(半透明の黒など)を付ける。**Raycast Target は OFF**。
3. 子に以下を配置する。
   - **`TrackArea`**: コースの線(横に細長い `Image`)。Anchor `middle-stretch` で左右に余白を取る。
     **左端=スタート、右端=ゴール** として扱われる(この RectTransform の幅がそのままコースの長さになる)。
   - **スタート線**: `TrackArea` の左端に細い縦線の `Image`(任意)。
   - **ゴール旗/ゴールライン**: `TrackArea` の右端に旗やチェッカーの `Image`(Anchor を右端 `X=1` にすると解像度が変わってもずれない)。

## 2. 目盛りプレハブ(任意)
1. 細い縦線の `Image`(例: 幅 2、高さ 20)を作る。Pivot は中央。
   - 縦方向の位置はプレハブの Anchor / Pos Y がそのまま使われる(横方向だけスクリプトが上書きする)。
2. 距離を表示したい場合は子に `TextMeshProUGUI` を置く(例: 線の下に小さく「残り4000m」)。
3. プレハブ化する。

## 3. アイコンプレハブ
1. UI オブジェクトを作り、Pivot を中央(0.5, 0.5)、サイズ 60×60 程度にする。
2. 子に以下を置く。
   - 動物イラスト用の `Image`(**Preserve Aspect を ON** 推奨)。
   - 出場番号用の `TextMeshProUGUI`(数字だけが入る。例: 右下に小さな丸の背景+数字)。
   - 任意: ゴール後に表示するオブジェクト(「GOAL」マークなど)。
3. ルートに `RaceMinimapIcon` を付けて各フィールドを設定し、プレハブ化する。
   - Raycast Target はすべて OFF でよい(クリックは使わない)。

## 4. RaceMinimap コンポーネント
1. `RaceMinimap` オブジェクト(または別の常にアクティブな GameObject)に `RaceMinimap` を付ける。
2. Inspector で設定する。
   | フィールド | 設定先 |
   |---|---|
   | Minimap Root | 1 で作った `RaceMinimap` オブジェクト |
   | Track Area | `TrackArea` |
   | Icon Prefab | 3 で作ったプレハブ |
   | Lane Offset Y | レーンごとの縦ずらし量(px)。接戦時にアイコンが重なりすぎないように |
   | Tick Prefab | 2 で作ったプレハブ(未設定なら目盛りなし) |
   | Tick Interval Meters | 目盛りの間隔(既定 1000m。コース全長は `RaceTrack.TrackLength` = 5000m) |
   | Tick Label Format | 目盛りの文字。`{0}`=スタートからの距離、`{1}`=ゴールまでの残り距離(既定「残り{1}m」) |
3. `RaceManager` の **Race Minimap** フィールドに、このコンポーネントを設定する。

## 動作
- 出走表画面中は非表示。`RaceManager.BeginRace()` で表示され、アイコンと目盛りが生成される。
- 前にいる走者ほど手前に描画される。ゴールした走者は右端(ゴール位置)で止まる。
- 全員ゴール後、結果画面が出るタイミングで非表示になる。
- `RaceManager` の Race Minimap が未設定でも、レースはこれまで通り動く。
