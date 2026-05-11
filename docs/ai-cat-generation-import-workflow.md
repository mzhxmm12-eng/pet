# AI 生成猫咪资产包并导入应用流程

## 目标

根据用户提供的猫咪照片，使用 AI 生成一套可直接放入应用的猫咪资产包。最终输出必须满足应用扫描规则：目录放入 `assets/pets/{cat-id}` 后，控制面板猫窝能识别、展示并切换运行。

## 输入信息

每次生成前先收集：

- 猫咪照片：至少 1 张清晰图。
- 猫咪名称：用于控制面板展示。
- `cat-id`：英文小写、数字和短横线，例如 `black-white-cat`。
- 外观描述：如奶牛猫、狸花猫、橘猫、布偶猫。
- 特征备注：眼睛颜色、脸部花纹、尾巴形态、特殊斑纹。

## 最终目录结构

```text
assets/pets/{cat-id}/
  manifest.json
  preview.png
  sprites/
    idle.png
    walk_right.png
    walk_left.png
    sleep.png
    play.png
    play_left.png
    eat.png
    reject.png
    dragged.png
```

## AI 生成总流程

### 1. 提取猫咪角色设定

先根据照片写一份角色设定，后续所有动作都必须复用这份设定。

输出格式：

```text
猫咪名称：
猫咪品种/外观：
毛色：
脸部特征：
眼睛颜色：
尾巴形态：
体型：
像素风关键词：
禁止变化的特征：
```

要求：

- 所有动作里猫咪花纹必须一致。
- 不要每个动作生成成不同猫。
- 不要改变眼睛颜色、脸部斑纹和尾巴形状。

### 2. 生成 preview.png

生成 64x64 单帧 PNG。

要求：

- 透明背景。
- 像素风。
- 猫咪正面或 3/4 侧面。
- 能清楚表现猫咪最重要的特征。

输出：

```text
assets/pets/{cat-id}/preview.png
```

### 3. 生成核心动作

优先生成这 4 个动作：

1. `idle`
2. `walk_right`
3. `sleep`
4. `play`

原因：

- `idle` 决定默认观感。
- `walk_right` 决定自动走动质量。
- `sleep` 决定长时间陪伴状态。
- `play` 决定逗猫体验。

### 4. 派生左右动作

生成完右向动作后：

- `walk_left` 由 `walk_right` 水平翻转。
- `play_left` 由 `play` 水平翻转。

除非右向动作翻转后明显不自然，否则不要重新让 AI 单独生成左向动作，避免左右动作不是同一只猫。

### 5. 生成反馈动作

继续生成：

- `eat`
- `reject`
- `dragged`

这些动作服务文件投喂和拖拽，不需要特别复杂，但要保持外观一致。

## 动作规格

| 文件 | Sprite Sheet 尺寸 | 单帧 | 帧数 | FPS | 循环 |
|---|---:|---:|---:|---:|---|
| preview.png | 64x64 | 64x64 | 1 | - | 否 |
| sprites/idle.png | 256x64 | 64x64 | 4 | 4 | 是 |
| sprites/walk_right.png | 384x64 | 64x64 | 6 | 8 | 是 |
| sprites/walk_left.png | 384x64 | 64x64 | 6 | 8 | 是 |
| sprites/sleep.png | 256x64 | 64x64 | 4 | 3 | 是 |
| sprites/play.png | 384x64 | 64x64 | 6 | 10 | 否 |
| sprites/play_left.png | 384x64 | 64x64 | 6 | 10 | 否 |
| sprites/eat.png | 384x64 | 64x64 | 6 | 8 | 否 |
| sprites/reject.png | 192x64 | 64x64 | 3 | 5 | 否 |
| sprites/dragged.png | 128x64 | 64x64 | 2 | 4 | 是 |

## AI 提示词模板

### 通用提示词

```text
Create a cute 64x64 pixel art desktop pet cat sprite.
The cat must match this character design:
{猫咪角色设定}

Style requirements:
- pixel art, cute desktop pet style
- match the approved existing sprite style exactly: same pixel density, soft shading, outline weight, head/body proportions, eye size, and cute desktop-pet readability as the latest accepted idle/walk sprites
- when an approved sprite already exists for this cat, use it as the style and identity reference; new actions must look like animation frames of that exact same cat, not a newly redesigned cat
- transparent background
- sharp readable silhouette at 64x64
- consistent face markings, body markings, eye color, and tail shape
- stable body size and foot anchor across frames
- frame the whole cat safely inside every 64x64 frame: ears, tail tip, back, belly, legs, and all paws must be fully visible and must not touch or be cropped by any edge
- if an action needs a high jump or stretched pose, scale the cat down slightly or shift it inward so the complete cat silhouette remains inside the frame
- keep every visible paw fully inside the 64x64 frame, with at least 2 pixels of transparent padding below the lowest paw
- do not draw random dark strokes, scars, stripes, or dirty pixels on the face unless they are part of the real cat's markings
- do not draw UI cursors, mouse pointers, target marks, motion guide marks, arrows, or helper symbols inside the sprite
- do not manually fake anatomy with thin stick-like legs; paws and legs must look naturally connected to the body
- no text, no UI, no background objects
- no realistic photo style, no 3D render
```

### idle

```text
Generate a horizontal sprite sheet for idle animation.
Canvas: 256x64, 4 frames, each frame 64x64.
Action: sitting or standing idle, subtle breathing, blink, small tail movement.
Keep the feet/body anchor stable across all frames.
All paws and the full lower body must remain visible; do not crop or hide the feet at the bottom edge.
Leave 6-10 pixels of bottom-safe transparent space when the sprite will be shown inside rounded UI containers.
If adding a blink, close the eyelids cleanly without creating black horizontal bars or random face stripes.
Transparent background.
```

### walk_right

```text
Generate a horizontal sprite sheet for walking right.
Canvas: 384x64, 6 frames, each frame 64x64.
Action: the cat walks to the right with clear alternating legs.
The body height and size must remain stable.
The foot contact point should not jump.
This must be a real walk cycle, not a sliding/static cat: front legs and back legs must visibly alternate forward and backward across frames while the torso remains visually anchored.
The walking paws must show front-back stepping motion: in different frames one front paw reaches forward while the opposite back paw trails backward, then they swap positions.
Do not keep all paws directly under the body; the feet must move forward and backward relative to the torso so the walk reads clearly at 64x64.
At least 4 of the 6 frames must have clearly different paw positions, including visible forward reach, rear push-off, passing/under-body, and swapped-foot phases.
The animation should still read as walking if all frame origins are overlaid: the foot pixels themselves must change position relative to the torso, not just the whole cat shifting inside the frame.
Use a reasonable quadruped gait: one front paw reaches while the opposite rear paw pushes, then those paws plant as the body passes over them, then the opposite front/rear pair repeats. At least 5 of 6 frames should show meaningful paw-phase changes rather than only two alternating drawings.
Legs must be short, compact, and attached under the torso. Avoid thin vertical line legs, detached paws, duplicate ghost paws, or hand-drawn replacement legs that do not match the body.
There must be no detached pixel fragments in front of or behind the walking cat. If a paw/tail fragment appears separated from the main silhouette at the leading edge, the frame fails and must be regenerated or cleaned.
Use the existing built-in cat walk sprite as pose reference: compact body, natural paw contact, alternating legs, no extra anatomy guides.
Transparent background.
```

### sleep

```text
Generate a horizontal sprite sheet for sleeping.
Canvas: 256x64, 4 frames, each frame 64x64.
Action: the cat curls up or lies down sleeping, with gentle breathing.
All sleep frames must keep the same facing direction and the same curled/lying pose family. Do not turn the cat around, switch to a back view, flip direction, sit up, or change camera angle during the sleep loop.
The sleep motion should be only subtle breathing, blinking, ear twitch, or tiny tail movement; the body anchor and silhouette should remain stable.
Must still clearly look like the same cat.
Transparent background.
```

### play

```text
Generate a horizontal sprite sheet for play/pounce animation.
Canvas: 384x64, 6 frames, each frame 64x64.
Action: the cat jumps or pounces toward the upper-right/front-right direction, front paws reaching out, tail lifted.
The motion should feel like chasing a cursor near the cat.
The play action must look like a diagonal upward jump, similar to the previous built-in play action: the cat pushes off from the ground, travels up and to the right, stretches forward in midair, then lands.
The motion arc must include clear vertical movement: crouch low, jump upward toward the cursor, reach the highest point, then land.
Do not make the cat only slide horizontally; the head/body should rise by roughly 10-18 pixels at the peak frame.
Keep the complete cat visible during the whole pounce. At the highest stretched frame, do not crop the ears, front paws, tail tip, belly, back legs, or landing paws; shrink or reposition the pose if needed.
Do not draw the cursor, mouse pointer, target, sparkle, arrow, or any black helper mark in the sprite sheet; only draw the cat.
Use the existing built-in cat play animation as pose reference: frame 1 crouches low, frames 2-4 stretch diagonally upward, frame 5 falls/lands, frame 6 returns to standing.
Transparent background.
```

### eat

```text
Generate a horizontal sprite sheet for eating.
Canvas: 384x64, 6 frames, each frame 64x64.
Action: the cat lowers its head and eats small cat food, with chewing feedback.
Do not add large props; keep focus on the cat.
Transparent background.
```

### reject

```text
Generate a horizontal sprite sheet for reject/refuse animation.
Canvas: 192x64, 3 frames, each frame 64x64.
Action: the cat shakes head, looks confused, or steps back slightly.
Transparent background.
```

### dragged

```text
Generate a horizontal sprite sheet for dragged animation.
Canvas: 128x64, 2 frames, each frame 64x64.
Action: the cat looks like it is being picked up or held while dragged.
Keep it cute, not distressed.
Transparent background.
```

## 透明背景处理

优先要求 AI 直接输出透明 PNG。

如果 AI 无法稳定输出透明背景，则使用纯色背景方案：

- 背景色使用纯绿色 `#00FF00` 或纯蓝色 `#0000FF`。
- 猫咪身上不能出现同色区域。
- 生成后用抠色脚本移除背景。
- 抠图后检查边缘是否残留色边。

## Sprite Sheet 后处理要求

每张图生成后必须检查：

- 宽高是否正确。
- 是否正好横向排列。
- 每帧是否 64x64。
- 背景是否透明。
- 猫咪没有被裁切。
- 每一帧都必须保留完整猫咪轮廓，耳朵、尾巴尖、身体、腿和脚掌不能贴边、缺角或被 64x64 边界裁掉。
- `idle` 的脚和身体底部没有被底边、圆角容器或透明裁切吃掉。
- `idle` 放进控制面板预览后，脚底和身体底部仍完整可见；如果容器有圆角遮挡，必须继续上移或缩小。
- 脸部没有多余黑线、脏线、乱码状像素或非角色设定里的条纹。
- 眼睛必须保持干净完整：琥珀色/原角色眼色、高光和瞳孔应可读，不能糊成黑块、断裂像素、脏点、残影或被脸部斑纹吞掉。
- 脚底锚点是否稳定。
- `walk_right` / `walk_left` 不能只是整只猫平移，腿和脚必须逐帧交替摆动。
- `walk_right` / `walk_left` 的脚掌必须有明确前后走动：至少能看到前脚前伸、后脚后蹬，再在后续帧互换位置。
- `walk_right` / `walk_left` 把每帧原点对齐叠加查看时，脚掌像素也必须相对身体发生前后变化；如果只是整只猫在 64x64 格子里位移，判定失败并重生。
- `walk_right` / `walk_left` 要符合合理四足步态：前脚前伸时对侧后脚后蹬，落脚/支撑/摆动阶段应连续，至少 5/6 帧有有效脚步相位变化，不能只在两张图之间来回切。
- `walk_right` / `walk_left` 的腿不能是细线、断开的白色竖条、幽灵脚、重复脚影或与身体不连接的手工补丁。
- `walk_right` / `walk_left` 前方或后方不能出现与主体分离的小残片、尾巴断片、脚掌残影或粘连色块；放大到 4-6 倍检查，发现即清理或重生。
- 从 AI 大图切割 `walk_right` / `walk_left` 时必须按每只猫的连通主体蒙版提取，不要只用矩形框裁切；矩形裁切容易把相邻帧的脸、尾巴或脚残片带入当前 64x64 帧。
- `sleep` 4 帧必须保持同一朝向和同一睡姿家族，只允许呼吸、眨眼、耳朵或尾巴轻微变化；不能突然转成背面、反方向、坐姿或站姿。
- `sleep` 循环播放时身体锚点、头部位置、尾巴位置应稳定，不能出现镜头/朝向跳变。
- `play` / `play_left` 必须有明显纵向跳跃弧线，不能只是横向滑动。
- `play` / `play_left` 必须像已有 play 动作一样斜上方跳跃：伏低起跳、向斜上方伸展、空中最高点、落地恢复都要清楚。
- `play` / `play_left` 不能包含鼠标光标、黑色箭头、目标标记、运动辅助线或任何 UI 元素。
- `play` / `play_left` 应对照已有内置猫咪 `play.png` 验收，动作节奏必须是伏低、斜向腾空、最高点、落地、站起。
- 同一动作内猫咪大小是否跳动。
- 不同动作之间是否像同一只猫。

## manifest.json 模板

```json
{
  "formatVersion": 1,
  "petId": "{cat-id}",
  "name": "{猫咪名称}",
  "species": "cat",
  "style": "pixel_cute",
  "author": "DeskPet",
  "preview": "preview.png",
  "canvas": {
    "width": 64,
    "height": 64,
    "scale": 2
  },
  "hitArea": {
    "x": 6,
    "y": 4,
    "width": 52,
    "height": 58
  },
  "anchor": {
    "x": 32,
    "y": 60
  },
  "animations": {
    "idle": {
      "file": "sprites/idle.png",
      "frameWidth": 64,
      "frameHeight": 64,
      "frames": 4,
      "fps": 4,
      "loop": true
    },
    "walk_left": {
      "file": "sprites/walk_left.png",
      "frameWidth": 64,
      "frameHeight": 64,
      "frames": 6,
      "fps": 8,
      "loop": true
    },
    "walk_right": {
      "file": "sprites/walk_right.png",
      "frameWidth": 64,
      "frameHeight": 64,
      "frames": 6,
      "fps": 8,
      "loop": true
    },
    "sleep": {
      "file": "sprites/sleep.png",
      "frameWidth": 64,
      "frameHeight": 64,
      "frames": 4,
      "fps": 3,
      "loop": true
    },
    "play": {
      "file": "sprites/play.png",
      "frameWidth": 64,
      "frameHeight": 64,
      "frames": 6,
      "fps": 10,
      "loop": false
    },
    "play_left": {
      "file": "sprites/play_left.png",
      "frameWidth": 64,
      "frameHeight": 64,
      "frames": 6,
      "fps": 10,
      "loop": false
    },
    "eat": {
      "file": "sprites/eat.png",
      "frameWidth": 64,
      "frameHeight": 64,
      "frames": 6,
      "fps": 8,
      "loop": false
    },
    "reject": {
      "file": "sprites/reject.png",
      "frameWidth": 64,
      "frameHeight": 64,
      "frames": 3,
      "fps": 5,
      "loop": false
    },
    "dragged": {
      "file": "sprites/dragged.png",
      "frameWidth": 64,
      "frameHeight": 64,
      "frames": 2,
      "fps": 4,
      "loop": true
    }
  }
}
```

## 导入应用步骤

1. 创建目录：

```text
assets/pets/{cat-id}/
```

2. 放入 `manifest.json` 和 `preview.png`。
3. 创建 `sprites` 子目录并放入所有动作图。
4. 确认 `manifest.json` 中的 `petId` 与目录名建议一致。
5. 运行构建：

```powershell
dotnet build .\DeskPet.sln -c Debug
```

6. 启动应用。
7. 打开控制面板。
8. 在猫窝中选择新猫咪。
9. 点击「应用设置」。

## 导入前验收清单

导入前必须满足：

- `manifest.json` 存在。
- `preview.png` 存在且为 64x64。
- 所有必需动作都存在。
- 所有 Sprite Sheet 尺寸正确。
- `species` 必须是 `cat`。
- `petId` 不能和已有猫咪重复。
- 背景透明。
- `walk_left` 和 `play_left` 方向正确。
- 猫咪没有明显变形或动作间换脸。

## 常见失败原因

- AI 生成的 Sprite Sheet 不是横向排列。
- 单帧不是 64x64。
- 图片实际有白底或色底。
- `manifest.json` 帧数和图片实际宽度不一致。
- 左右动作缺失。
- `petId` 重复。
- `species` 写错，导致控制面板猫窝不显示。
- 走路动作脚底不稳定，播放时猫咪大小跳动。
- 走路动作只有身体位置变化，没有腿部前后摆动，看起来像平移。
- 为了修走路而手动画腿，导致出现细长竖线腿、断腿、脚掌漂浮或多余白色腿影。
- 待机动作贴近 64x64 底边，放入控制面板圆角容器后脚被遮住。
- 使用画线模拟眨眼或表情时，在脸上留下黑色横线、条纹或脏像素。
- 逗猫动作没有纵向起跳，只是向光标方向横向滑动。
- 逗猫 sprite 中把鼠标光标、黑色箭头、目标点或运动提示符也画了进去。
- 逗猫最后一帧尾巴、耳朵或身体边缘被裁切，形成悬浮黑条或残片。

## 推荐生成顺序

实际操作时按这个顺序最稳：

1. 照片分析，写角色设定。
2. 生成 `preview.png`，确认像目标猫。
3. 生成 `idle.png`，确认默认观感。
4. 生成 `walk_right.png`，确认走路自然。
5. 翻转得到 `walk_left.png`。
6. 生成 `sleep.png`。
7. 生成 `play.png`。
8. 翻转得到 `play_left.png`。
9. 生成 `eat.png`、`reject.png`、`dragged.png`。
10. 生成 `manifest.json`。
11. 放入 `assets/pets/{cat-id}`。
12. 构建并在控制面板中验收。
