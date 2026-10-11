# 動物検索機能 セットアップ手順

`AnimalSearchManager` はスクリプトのみで、UIはBaseSceneにエディタで配置する。

## 1. 検索パネル
1. Canvas の下に空の UI オブジェクト `SearchPanel` を作り、全画面に広げる（Anchor: stretch / stretch）。
2. `Image`（半透明の黒など）を付け、**Raycast Target を ON** にする。
   - パネル表示中に背後の動物がクリックされる（PhysicsRaycaster）のを防ぐため。
3. `CanvasGroup` と `UIPanelAnimator` を付ける（`hideOnAwake` は ON）。
4. `SearchPanel` の中に以下を配置する。
   - **TMP - Input Field**（キーワード入力欄）
   - **モード切替ボタン**（子に TextMeshProUGUI。このテキストが「なまえ / さくしゃ」になる）
   - **検索ボタン**
   - **閉じるボタン**（任意）
   - **メッセージ用 TextMeshProUGUI**（「見つかりませんでした」等を表示）
   - **Scroll View**。`Content` に `VerticalLayoutGroup` と `ContentSizeFitter`（Vertical Fit: Preferred Size）を付ける。

## 2. 候補行プレハブ
1. `Button` を持つ UI オブジェクトを作り、子に名前用・作者用の TextMeshProUGUI、任意でサムネイル用 `Image` を置く。
   `VerticalLayoutGroup` の子になるので `LayoutElement` で高さ（例: Preferred Height 80）を指定する。
2. ルートに `AnimalSearchCandidateItem` を付け、各フィールドを設定する。
3. `Button.onClick` に **自分自身の `AnimalSearchCandidateItem.OnClicked`** を設定してプレハブ化する。

## 3. AnimalSearchManager
1. シーンに空の GameObject（例: `AnimalSearchManager`）を作り、`AnimalSearchManager` を付ける。
2. Inspector で設定する。
   | フィールド | 設定先 |
   |---|---|
   | Selection Manager / Illustration Manager / Camera Mover / Option Manager | シーン上の各コンポーネント |
   | Search Panel | `SearchPanel` の `UIPanelAnimator` |
   | Input Field | 入力欄 |
   | Mode Label | モード切替ボタンのテキスト |
   | Message Text | メッセージ用テキスト |
   | Candidate Container | Scroll View の `Content` |
   | Candidate Prefab | 2 で作ったプレハブ |
   | Open Key | 検索画面を開くキー（既定: F） |

## 4. ボタン等の配線（Inspector の永続コール）
| UI | イベント | メソッド |
|---|---|---|
| 画面上の🔍ボタン（パネルの外に置く） | `onClick` | `AnimalSearchManager.ToggleSearch` |
| 入力欄 | `On Submit (String)` | `AnimalSearchManager.OnSearchSubmitted`（**Dynamic string** を選ぶ） |
| 検索ボタン | `onClick` | `AnimalSearchManager.OnSearchButtonClicked` |
| モード切替ボタン | `onClick` | `AnimalSearchManager.ToggleSearchMode` |
| 閉じるボタン | `onClick` | `AnimalSearchManager.CloseSearch` |

## 注意
- **フォント**: `NotoSansJP-Medium SDF` は Static アトラスのため、入力された漢字などが □ になることがある。
  入力欄・候補・メッセージのテキストには Dynamic フォント（`Assets/SU3DJPFont/TextMeshProFont/Dynamic/` のもの、
  または Noto の Atlas Population Mode を Dynamic にしたもの）を使う。
- `FGUIStarter.CustomButton` を使う場合は子に TextMeshProUGUI が必須（ないと `Awake()` で例外になる）。
- 開発モード（`IllustrationManager.isDevelopment = true`）では、テスト画像の作者名はすべて「テストさくしゃ」になる。
  さくしゃモードで「てすと」と検索すると候補リストの表示を確認できる。

## 操作
- F キー / 🔍ボタンで開く、Esc / 閉じるボタンで閉じる。開いている間はカメラ操作（WASD・Space・P・マウス）が無効。
- Enter / 検索ボタンで検索。部分一致で、ひらがな/カタカナ・全角/半角・英大小文字は区別しない。
- 1件ヒット → その動物にフォーカス。複数ヒット → 候補から選ぶとフォーカス。
