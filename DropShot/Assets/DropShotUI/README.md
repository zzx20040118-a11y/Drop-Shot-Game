# 标题页 + 结局页（DropShot.UI）v2

v2 按 figma 里的开始页面草图、标题动画草稿和结局页草稿重新排版，换上了组里画好的美术。

| 页面 | 布局 | 动效 |
| --- | --- | --- |
| 标题页 | 场景 1 上色背景；左边 DROP / SHOT 双行 Logo，右边巧克力螺旋盘；Logo 下方是球座 | Logo 掉落弹跳 → **甜甜圈 loading**（后台真加载游戏场景）→ **loading complete**（压扁、弹起旋转、糖针炸开）→ 球座弹出、球掉到球座上 → **START / QUIT 按钮动效**（弹出、呼吸、悬停、按下、果冻回弹）→ 点 START：**球被击飞，沿弧线飞进螺旋，螺旋加速旋转并放大**，进入游戏。螺旋按 122 BPM 轻轻跳动 |
| 结局页 | 按结局页草稿：左边糖罐，罐口挂着 PERFECT / GOOD / MISS 三条丝带，右边分数云朵，右下 BACK 气泡 | 糖罐掉落 → 云朵升起 → **分数加载动画**（总分滚动计数 → 三条丝带依次展开并计数 → 最大连击 → 准确率 → 评级印章砸下、糖针炸开、云朵一震）→ **BACK 按钮动效**。播放中按空格或点击可快进 |

Unity 6000.3 下测试通过（新版 Input System），已自动跑通「标题 → 游戏 → 结局 → 回标题」。

## 用到的美术

| 来源 | 用在 |
| --- | --- |
| figma「游戏场景1 / 2」上色图（米粒） | 标题页、结局页背景 |
| figma 巧克力螺旋上色图 | 标题页螺旋盘（以螺旋中心切成圆盘，缝隙填奶油色） |
| figma「反馈字体」果冻字 | 结局页 PERFECT / GOOD / MISS 丝带上的字 |
| 结局页草稿：糖罐、BACK 气泡、分数云朵 | 结局页（罐身和云朵填了奶油色；云朵里的占位字「XXX / xx」已抹掉，换成实时数字） |
| 标题动画草稿（录屏） | 球座 + 击球飞进螺旋的转场 |

草稿里的丝带标签带了「xx」占位字，没法直接叠数字，所以按草稿的位置和角度重新做成了可以填数字的丝带。组里的 3D 甜点模型属于游戏场景，这个包没有用到。

## 前提

项目里已经导入过 `DropShotJudgement`（判定模块）。

## 接入（5 分钟）

1. 导入 `DropShot_TitleEnd.unitypackage`。第一次弹出「Import TMP Essentials」就点 Import。
2. **标题**：直接用 `Assets/DropShotUI/Scenes/Title.unity`。
3. **结局**：打开现有的 `End` 场景，把 `Assets/DropShotUI/Prefabs/ResultScreen.prefab` 拖进去（场景里需要一个 Camera）。
4. **游戏场景**（HalftoneEffect）：任意物体上加组件 **ScoreReporter**，成绩才能带到结局页。
5. **Build Settings** 顺序：`Title`（第一个）→ `HalftoneEffect` → `End`。

场景名不一样的话，在 Inspector 里改 TitleScreen 的 `Game Scene Name`、ResultScreen 的 `Title Scene Name`。

## 按键

| 页面 | 操作 |
| --- | --- |
| 标题 | 空格 / 点击 = START；Esc = QUIT |
| 结局 | 空格 / 点击 = 快进；结束后 空格 = BACK |

## 之后换美术

- 背景、螺旋盘、糖罐、云朵、BACK 气泡都是 Prefab 里的 Image，直接换 Sprite 不影响动效。
- Logo 现在是描边文字；开始页面草图里的「DROP SHOOT」糖果字上色后，可以在 `Logo` 物体下换成 Image。
- 评级：准确率（Perfect 记 1，Good 记 0.5）≥95% S、≥85% A、≥70% B，其余 C。

## 音效

用的是任务 7 环境音频包里的 UI 音：倒数嘀嗒（loading / 计数）、重音嘀嗒（完成）、落地声（Logo / 球落地、盖章）、combo 铃声（按钮 / 评级）、起飞呼啸（击球进入游戏）。
