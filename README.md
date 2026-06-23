# Unity Editor 拡張機能 README

---

## 概要

本リポジトリには、Unityエディター上での開発効率を向上させる、3つのエディター拡張機能が含まれています。

---

## 1. MissingReferenceHighlighter

### 概要

Hierarchyウィンドウ上で、**シリアライズフィールドに未アサイン（None）の参照が存在するGameObjectを赤くハイライト**表示する拡張機能です。アサイン漏れを視覚的に素早く発見できます。

### 機能

- Hierarchyウィンドウのアイテム描画時に、各GameObjectのコンポーネントを自動検査
- `[SerializeField]` が付いたフィールド、およびパブリックフィールドを対象に未アサインを検出
- `List<T>` や配列などのコレクション型にも対応し、要素のいずれかがNoneであればハイライト
- 一度解析した型のフィールド情報をキャッシュし、パフォーマンスへの影響を最小限に抑制
- 自分で作成したスクリプト（`Assembly-CSharp`）のみを検査対象とし、Unityの標準コンポーネントやサードパーティパッケージは除外

### 使い方

スクリプトをプロジェクトの `Editor` フォルダ配下に配置するだけで、自動的に有効になります。特別な操作は不要です。未アサインのフィールドが存在するGameObjectは、Hierarchyウィンドウ上で背景が薄く赤くハイライトされます。

---

## 2. ScriptCreateLauncher

### 概要

Projectウィンドウの右クリックメニューから、**名前空間・クラス名・スクリプト種別を指定してC#スクリプトを生成**できる拡張機能です。Unityのデフォルトのスクリプト作成と異なり、名前空間を最初から指定した状態でファイルを生成できます。

### 機能

- `Assets/Create/Custom C# Script` メニューから専用ウィンドウを起動
- 作成可能なスクリプト種別は以下の3種類
  - **MonoBehaviour** : 通常のコンポーネントスクリプト
  - **PureClass** : MonoBehaviourを継承しない純粋なC#クラス
  - **ScriptableObject** : `[CreateAssetMenu]` 属性付きのScriptableObject
- 名前空間入力時に、プロジェクト内の既存の名前空間をサジェスト表示（最大5件）
- 選択中のフォルダに直接ファイルを生成し、Projectウィンドウにフォーカス

### 使い方

1. Projectウィンドウで任意のフォルダを右クリック（または選択）
2. `Assets > Create > Custom C# Script` を選択
3. 表示されたウィンドウで名前空間・クラス名・スクリプト種別を入力
4. `Create Script` ボタンを押すとスクリプトが生成されます

---

## 3. DebugButtonManager / DebugButtonAttribute

### 概要

メソッドに `[DebugButton]` 属性を付けるだけで、**ゲーム実行中に画面上のデバッグUIからそのメソッドを呼び出せる**拡張機能です。Inspectorのコンテキストメニュー（`[ContextMenu]`）のランタイム版に相当します。

### 機能

- ゲーム実行時に自動で起動し、`[DebugButton]` 属性が付いたメソッドをシーン全体から自動収集
- 引数として `int` / `float` / `string` / `bool` / `Enum` 型に対応し、UI上から値を入力してメソッドを呼び出し可能
- ウィンドウは最小化に対応しており、プレイ中の画面を邪魔しにくい設計
- `DontDestroyOnLoad` により、シーン遷移後もデバッグUIが維持される
- 自分で作成したスクリプト（`Assembly-CSharp`）のみを対象とし、不要なコンポーネントは除外

### 使い方

**1. ファイル構成**

| ファイル | 配置先 |
|---|---|
| `DebugButtonAttribute.cs` | `Assets` 以下の任意の場所 |
| `DebugButtonManager.cs` | `Assets` 以下の任意の場所（`Editor` フォルダ不可） |

**2. 属性を付ける**

```csharp
using St.EditorExtension;

public class SampleScript : MonoBehaviour
{
    [DebugButton]
    private void ResetPosition()
    {
        transform.position = Vector3.zero;
    }

    [DebugButton("HP回復")]
    private void HealPlayer(int amount)
    {
        hp += amount;
    }
}
```

**3. ゲームを再生する**

画面左上にデバッグウィンドウが自動表示されます。ボタンを押すとメソッドが即座に呼び出されます。

### 引数UIの対応型

| 型 | UI |
|---|---|
| `int` / `float` | テキストフィールド |
| `string` | テキストフィールド |
| `bool` | トグルボタン（True / False） |
| `Enum` | クリックで順番に切り替え |

## 動作確認済み環境

- **Unity 6**

---

## 導入方法

各スクリプトをプロジェクト内の任意の `Editor` フォルダ配下に配置してください。

---

## 名前空間

どちらのスクリプトも `St.EditorExtension` 名前空間に属しています。
