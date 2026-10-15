# オプション画面「表示上限」「出走数」のプラス・マイナスボタン セットアップ手順

表示上限と出走数は、スライダーではなく **マイナスボタン / 数値ラベル / プラスボタン** で調整する。
スクリプトのみ用意しているので、UIは `Assets/99_Common/Prefabs/Option.prefab` をエディタで開いて配置する。

- 最小値のときはマイナスボタン、最大値のときはプラスボタンが自動で押せなくなる（`Button.interactable = false`）。
  押せないときの見た目は各 Button の `Transition`（Disabled Color / Disabled Sprite）で設定する。
- `UIRepeatButton` を付けると長押しで連続して増減する（タップは1ずつ）。

| 項目 | 範囲 / 初期値 | 定義場所 | 保存先 (PlayerPrefs) |
|---|---|---|---|
| 表示上限 | 1〜100 / 30 | `IllustrationManager` の Inspector | `MaxDisplayAnimalCount` |
| 出走数 | 2〜50 / 20 | `RacerCountSetting` の定数 (`Assets/99_Common/Scripts/RaceSettings/`) | `RacerCount` |

どちらもキャッシュクリアで初期値に戻る。表示中の動物が出走数より少ない場合は、表示中の動物が全員出走する。

## 1. 古い表示上限スライダーを削除する
1. `Option.prefab` を開き、表示上限のスライダー（onValueChanged に `OptionManager.OnMaxDisplayCountChanged` が設定されていたもの）を削除する。
   - メソッドは削除済みなので、残すとInspectorで Missing 表示になる。
   - 数値ラベル（今まで `Max Display Count Label` に設定していたテキスト）は流用してよい。

## 2. 行を作る（表示上限・出走数の2行）
1. 1行ぶんの空の UI オブジェクトを作り、`HorizontalLayoutGroup` を付ける。
2. 子に以下を並べる。
   - 項目名の TextMeshProUGUI（「表示上限」「出走数」）
   - **マイナスボタン**（子のテキストは「−」）
   - **数値ラベル**（TextMeshProUGUI）
   - **プラスボタン**（子のテキストは「＋」）
   - ボタンにプロジェクトの `FGUIStarter.CustomButton` を使う場合は、子に TextMeshProUGUI が必須（無いと `Awake()` で NullReferenceException）。
3. 長押しで連続して増減させたい場合は、プラス・マイナスの各ボタンに **`UIRepeatButton`** を追加する。
   - Initial Delay（長押し判定までの秒数）/ Start Interval・Min Interval（連続入力の間隔）/ Acceleration Time（最短間隔になるまでの秒数）で速さを調整できる。

## 3. OptionManager の Inspector
| フィールド | 設定先 |
|---|---|
| Max Display Count Minus Button / Plus Button | 表示上限の行のマイナス / プラスボタン |
| Max Display Count Label | 表示上限の行の数値ラベル |
| Racer Count Minus Button / Plus Button | 出走数の行のマイナス / プラスボタン |
| Racer Count Label | 出走数の行の数値ラベル |

## 4. ボタンの配線（Inspector の永続コール）
| UI | イベント | メソッド |
|---|---|---|
| 表示上限のマイナスボタン | `Button.onClick` | `OptionManager.DecreaseMaxDisplayCount` |
| 表示上限のプラスボタン | `Button.onClick` | `OptionManager.IncreaseMaxDisplayCount` |
| 出走数のマイナスボタン | `Button.onClick` | `OptionManager.DecreaseRacerCount` |
| 出走数のプラスボタン | `Button.onClick` | `OptionManager.IncreaseRacerCount` |

`UIRepeatButton` は同じボタンの `onClick` を繰り返し呼ぶだけなので、追加の配線は不要。

## GameScene 側
`RaceManager` の Inspector にあった「出走数」(`racerCount`) は削除し、`RacerCountSetting.Get()` を読むようにした。
GameScene 側で設定する項目は無い。
