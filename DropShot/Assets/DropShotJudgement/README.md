# 判定与命中反馈模块（DropShot.Judgement）

读谱面 JSON → 按 D / F / 空格 判定 Perfect / Good / Miss → 播放命中音效、显示判定文字、连击和得分。
音效已用素材库里的文件接好，导入就能玩。Unity 6000.3 下测试通过。

## 先玩一下（1 分钟）

1. 双击 `DropShotJudgement.unitypackage` 导入。
2. 打开 `Assets/DropShotJudgement/JudgementDemo.unity`，点 Play。
3. 跟着 Gameotoon：**D / F 点击**；**空格按住蓄力，在下一拍松开**。左上角显示每一下的判定和偏差毫秒（F1 隐藏）。

> 第一次打开如果弹出「Import TMP Essentials」，点 Import 即可。

## 放进自己的场景

把 `DropShotJudgement.prefab` 拖进场景，然后在它的 **JudgementSystem** 上：
- `Chart Json` 换成子昕的谱面（现在是示例谱面 `demo_chart_gameotoon.json`）
- 如果场景里已经有放音乐的 AudioSource，把它拖到 `Music`，并取消那个 AudioSource 的 Play On Awake

## 包里有什么

| 内容 | 说明 |
| --- | --- |
| `DropShotJudgement.prefab` | 判定器 + 反馈 + 音效 + 判定文字 UI + 调试面板，全部接好 |
| `JudgementDemo.unity` | 示例场景 |
| `demo_chart_gameotoon.json` | 示例谱面：按 Gameotoon 鼓点生成，105 个音符（99 Normal、3 Long、3 Strike），格式与子昕的一致 |
| `Audio/` | Gameotoon、Perfect（Rainbow Combo Hit）、Good（Punch 03）、Miss（Near Miss，已截短）、Perfect 叠加（Glass Shatter 3）、Strike 蓄力（Power Charge） |
| `Scripts/` | 判定逻辑与反馈脚本；菜单 **DropShot > 判定自检** 可检查逻辑 |

## 判定规则（Inspector 里可调）

| 判定 | 偏差 |
| --- | --- |
| Perfect | ≤ ±50 ms |
| Good | ≤ ±100 ms |
| Miss | 超出 Good 或没按 |

- **Normal**：按下判定。早 150 ms 以上的按键忽略，不吃音符。
- **Long**：按下判定一次，按住到结束自动成功；提前松开超过 100 ms 算断。
- **Strike**：在 `time` 按住空格蓄力，在 `targetTime` 松开，按松开的偏差判定。
- 连击：Perfect / Good +1，Miss 清零。得分 Perfect 300、Good 100。

## 和其他代码对接

- 音符消失：订阅 `JudgementSystem.Judged`，事件里的 `noteIndex` = 谱面 `notes` 数组下标。
- 已有统一歌曲时钟：`judge.externalSongTime = () => 你的时间;`
- 总是按早 / 按晚：看调试面板的偏差，调 `Audio Offset Ms`。

## 需要子昕确认

1. `time` 是「应该击中的时间」吗？（目前按击中时间判定）
2. Strike 是「`time` 按下、`targetTime` 松开」吗？
3. `lane` 0 = D、1 = F 对吗？
