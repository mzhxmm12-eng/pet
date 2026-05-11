# 新猫咪素材制作与接入流程

## 目标

当用户提供一种新猫咪的图片后，可以按照统一流程完成：

1. 提取猫咪外观特征。
2. 生成桌宠所需 Sprite Sheet。
3. 编写宠物 manifest。
4. 放入应用资产目录。
5. 在控制面板中被识别和选择。

## 输入材料

用户至少需要提供：

- 猫咪清晰照片 1 张。
- 猫咪名称。
- 猫咪品种或外观描述，例如奶牛猫、橘猫、狸花猫、布偶猫。

建议照片要求：

- 猫咪主体清楚，无遮挡。
- 能看清脸部、耳朵、身体花纹和尾巴。
- 最好为坐姿或站姿全身照。

## 输出目录规范

每只猫使用一个独立目录：

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

后续如果按物种分组，可以迁移为：

```text
assets/pets/cats/{cat-id}/
```

但第一版可以继续使用当前目录结构。

## 必需素材清单

| 动作 | 文件 | 单帧尺寸 | 帧数 | FPS | 循环 | 用途 |
|---|---|---:|---:|---:|---|---|
| 预览图 | preview.png | 64x64 | 1 | - | 否 | 控制面板宠物卡片 |
| 待机 | sprites/idle.png | 64x64 | 4 | 4 | 是 | 默认状态 |
| 向右走 | sprites/walk_right.png | 64x64 | 6 | 8 | 是 | 自动走动 |
| 向左走 | sprites/walk_left.png | 64x64 | 6 | 8 | 是 | 自动走动，可由右走水平翻转 |
| 睡觉 | sprites/sleep.png | 64x64 | 4 | 3 | 是 | 自动睡觉 |
| 向右扑 | sprites/play.png | 64x64 | 6 | 10 | 否 | 鼠标逗猫向右互动 |
| 向左扑 | sprites/play_left.png | 64x64 | 6 | 10 | 否 | 鼠标逗猫向左互动，可由 play 翻转 |
| 吃东西 | sprites/eat.png | 64x64 | 6 | 8 | 否 | 文件投喂成功 |
| 拒绝 | sprites/reject.png | 64x64 | 3 | 5 | 否 | 投喂失败 |
| 被拖拽 | sprites/dragged.png | 64x64 | 2 | 4 | 是 | 用户拖动宠物 |

当前版本不再需要 `alert.png` 作为必需动作。

## 素材制作要求

### 通用要求

- 所有 Sprite Sheet 使用 PNG。
- 背景必须透明。
- 每帧固定 64x64。
- Sprite Sheet 横向排列，宽度等于 `64 * 帧数`，高度固定 64。
- 猫咪脚底或身体锚点要稳定，避免播放时大小跳动。
- 猫咪外观必须保持一致：毛色、脸部花纹、眼睛颜色、尾巴形态。

### 生成风格

建议统一为：

- 可爱像素风。
- 64x64 小尺寸可读。
- 边缘清晰。
- 动作夸张但不要变形到不像猫。

### 动作要点

- `idle`：轻微呼吸、眨眼、尾巴小幅摆动。
- `walk_right`：四肢交替明显，身体高度稳定，默认朝右。
- `walk_left`：通常由 `walk_right` 水平翻转生成。
- `sleep`：趴下或蜷缩，呼吸起伏，保持猫形。
- `play`：向右上方或右前方扑，前爪伸出，尾巴上扬。
- `play_left`：由 `play` 水平翻转，表现向左扑。
- `eat`：低头吃猫粮或咀嚼反馈。
- `reject`：摇头、疑惑或小后退。
- `dragged`：身体被提起或暂停姿态，适合用户拖动时播放。

## manifest 编写规范

每只猫必须提供 `manifest.json`：

```json
{
  "formatVersion": 1,
  "petId": "new_cat_id",
  "name": "猫咪名称",
  "species": "cat",
  "style": "pixel_cute",
  "author": "DeskPet",
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

## 从图片到素材的流程

### 1. 提取猫咪特征

根据用户图片总结：

- 毛色和主要花纹。
- 脸部特征。
- 眼睛颜色。
- 尾巴形态。
- 体型感觉。

这些特征要贯穿所有动作。

### 2. 生成 preview

生成一张 64x64 透明背景预览图，表现猫咪最有识别度的正面或半侧面形象。

输出：

```text
assets/pets/{cat-id}/preview.png
```

### 3. 生成基础动作

优先生成：

1. `idle`
2. `walk_right`
3. `sleep`
4. `play`

这 4 个决定猫咪的核心观感。

### 4. 派生左右动作

- `walk_left` 由 `walk_right` 水平翻转。
- `play_left` 由 `play` 水平翻转。

翻转后需要检查：

- 脚底锚点是否稳定。
- 方向是否正确。
- 画面是否被裁切。

### 5. 生成反馈动作

继续生成：

- `eat`
- `reject`
- `dragged`

这些用于文件投喂和拖拽体验。

### 6. 检查 Sprite Sheet 尺寸

每张图必须满足：

```text
实际宽度 = 64 * frames
实际高度 = 64
```

例如：

- idle：256x64
- walk_right：384x64
- walk_left：384x64
- sleep：256x64
- play：384x64
- play_left：384x64
- eat：384x64
- reject：192x64
- dragged：128x64

### 7. 编写 manifest

复制模板后修改：

- `petId`
- `name`
- `author`
- 动作文件路径
- 帧数、FPS、循环配置
- `hitArea`

`hitArea` 会影响逗猫触发区域，需要尽量贴近猫本体。

### 8. 放入应用

将完整目录放入：

```text
assets/pets/{cat-id}/
```

后续控制面板开发完成后，应用应扫描 `assets/pets` 下的 manifest，并按 `species = cat` 放入猫窝列表。

### 9. 验收测试

接入后需要检查：

- 控制面板能显示 preview。
- 选择猫咪后桌宠能正常显示。
- idle、walk、sleep、play、eat、reject、dragged 都能播放。
- 走路时大小不跳动，落脚点稳定。
- 向左/向右逗猫方向正确。
- 文件投喂成功播放 eat，失败播放 reject。
- 拖动宠物时播放 dragged。

## 后续自动化建议

后续可以增加一个素材导入工具，自动完成：

- 创建宠物目录。
- 校验 Sprite Sheet 尺寸。
- 生成 manifest。
- 生成 walk_left 和 play_left 翻转图。
- 在控制面板中刷新宠物列表。

第一版可以先手工生成和接入，保证规范稳定后再做导入工具。
