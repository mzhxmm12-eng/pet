# 网页端宠物形象生成需求文档

## 1. 目标

网页端宠物形象生成器负责将用户上传的猫咪或狗狗照片，生成可导入 Windows 桌宠客户端的像素宠物资产包。

MVP 输出内容：

1. 宠物像素形象。
2. 六组动作帧：
   - `idle`：待机，2-4 帧。
   - `walk_left`：向左走，4-8 帧。
   - `walk_right`：向右走，4-8 帧。
   - `sleep`：睡觉，2-4 帧。
   - `alert`：注意光标，1-3 帧。
   - `play`：扑/玩耍，3-6 帧。
3. 每组动作的 GIF 预览。
4. 一个 `.deskpet` 宠物资产包。

核心验收标准：用户能一眼觉得“这是我家的猫/狗”，而不是一只普通像素宠物。

## 2. 产品流程

```text
上传宠物照片
  -> 选择宠物类型
  -> 自动识别关键特征
  -> 用户确认/微调特征
  -> 生成基础像素形象
  -> 生成动作帧
  -> 生成 GIF 预览
  -> 用户选择版本
  -> 导出 .deskpet 资产包
```

## 3. 推荐技术栈

### 3.1 前端

推荐：`Next.js + React + TypeScript`

主要职责：

- 图片上传、裁剪和压缩。
- 展示生成进度。
- 展示候选形象。
- 播放 GIF 或 Sprite Sheet 预览。
- 提供简单特征微调表单。
- 导出 `.deskpet` 文件。

推荐库：

- UI：Tailwind CSS 或 shadcn/ui。
- 上传：Uppy、React Dropzone 或原生 file input。
- 图片裁剪：react-easy-crop。
- 动画预览：Canvas 或 CSS background-position 播放 Sprite Sheet。

### 3.2 后端

推荐：`Python + FastAPI`

主要职责：

- 接收上传图片。
- 调用图像生成模型或 ComfyUI 工作流。
- 做图片预处理、裁剪、透明背景、像素化后处理。
- 生成 GIF 预览。
- 打包 `.deskpet` 文件。

推荐组件：

- 任务队列：Celery + Redis，或 RQ + Redis。
- 图像处理：Pillow + OpenCV。
- GIF 生成：Pillow 或 ffmpeg。
- 文件存储：本地开发用磁盘；线上用 S3/R2/OSS。
- 数据库：PostgreSQL 或 Supabase；MVP 也可以先只存任务记录。

### 3.3 生图引擎

推荐保留两种实现路线：

#### 路线 A：外部图像生成 API

适合 MVP 快速验证，优势是上线快、维护少。

可使用支持图像输入、图像编辑、风格转换的模型 API。流程是上传宠物照片作为参考图，再用提示词要求生成像素风角色和动作帧。

优点：

- 开发速度快。
- 不需要自维护 GPU。
- 方便快速比较不同模型效果。

缺点：

- 成本随生成次数增长。
- 多帧一致性不可完全控制。
- 生成失败时需要重试和人工筛选。

#### 路线 B：ComfyUI / Stable Diffusion 工作流

适合后续稳定生产，优势是可控性更强。

建议使用固定工作流：

1. 宠物主体抠图。
2. 参考图特征提取。
3. 像素风格化。
4. 生成基础形象。
5. 使用动作模板或姿态约束生成各动作帧。
6. 后处理统一尺寸、透明背景、调色板。

优点：

- 可沉淀稳定工作流。
- 单次成本可控。
- 可以针对猫/狗动作单独优化。

缺点：

- 初期调参成本高。
- 需要 GPU 资源。
- 工作流维护复杂。

## 4. 关键技术策略

## 4.1 不建议直接独立生成每一帧

不要让模型分别生成 20-30 张独立图片后直接拼动画。这样很容易出现：

- 每一帧花纹不一致。
- 脸型和眼睛变化。
- 尺寸和位置漂移。
- 动起来像不同的猫/狗。
- 透明边缘和像素风格不统一。

更推荐的方式是“三步生成”：

1. 先生成稳定的宠物身份。
2. 再生成或套用动作姿态。
3. 最后统一后处理为标准 Sprite。

## 4.2 MVP 推荐生成方案

MVP 推荐使用“AI 生成基础形象 + 模板动画派生”的混合方案。

### 第一步：宠物身份提取

从用户上传的 1-5 张图片中提取关键特征：

- 宠物类型：猫/狗。
- 毛色：橘、白、黑、灰、奶牛、狸花、三花等。
- 花纹位置：额头、脸颊、背部、尾巴。
- 眼睛颜色。
- 耳朵形态。
- 体型：圆润、修长、幼年、成年。
- 特殊标记：白手套、白围脖、异瞳、短尾等。

这一步可以由视觉模型自动生成，也可以让用户在表单里确认。

### 第二步：生成标准像素形象

生成一个标准站立形象，推荐规格：

- 画布：64x64 或 96x96。
- 背景：透明。
- 风格：可爱像素卡通。
- 朝向：默认向右。
- 保留真实宠物的核心毛色和花纹。

建议一次生成 4 个候选，让用户选择最像的一版。

### 第三步：派生动作帧

对选中的基础形象生成动作帧：

- `idle`：身体轻微起伏、眨眼、尾巴微动。
- `walk_left`：从 `walk_right` 水平翻转得到，除非宠物花纹强依赖方向。
- `walk_right`：4-8 帧步行动画。
- `sleep`：趴下或蜷缩，呼吸起伏。
- `alert`：抬头、耳朵竖起、看向光标。
- `play`：前扑、伸爪、尾巴上扬。

为了稳定，动作帧可以使用模板骨架：

1. 猫/狗各准备一套通用动作模板。
2. 将用户宠物的主色、花纹、头部特征映射到模板上。
3. 对每帧做统一像素化和调色板约束。

这个方案比“每帧纯 AI 生成”更像同一只宠物。

## 5. 生图流程设计

## 5.1 输入

用户上传：

- 1-5 张宠物照片。
- 至少 1 张正脸或半身清晰照片。
- 可选：侧面、趴着、睡觉照片。

前端限制：

- 支持 JPG/PNG/WebP。
- 单张最大 10MB。
- 上传前压缩到最长边 1600px。
- 提示用户避免模糊、遮挡、多人/多宠物同框。

## 5.2 预处理

后端处理：

1. 检测宠物主体。
2. 裁剪到主体区域。
3. 去背景或弱化背景。
4. 统一输入尺寸。
5. 生成缩略图。

可选增强：

- 如果照片里有多只宠物，让用户框选目标宠物。
- 如果照片质量差，提示用户补充照片。

## 5.3 Prompt 结构

建议将提示词拆成结构化字段，而不是直接拼一句自然语言。

示例：

```text
Create a cute pixel art desktop pet sprite based on the reference pet.
Species: cat.
Main features: orange tabby, white chest, green eyes, round face, fluffy tail.
Style: adorable 2D pixel art, clean outline, limited color palette, transparent background.
Canvas: 64x64.
Pose: standing, facing right.
Constraints: preserve the pet's coat color and markings, no accessories, no text, no background.
```

动作帧 Prompt 示例：

```text
Create a 6-frame pixel art sprite sheet of the same pet walking to the right.
Keep identity, coat markings, color palette, face shape, and tail shape consistent.
Canvas per frame: 64x64.
Layout: horizontal sprite sheet, 6 frames, transparent background.
Style: cute 2D pixel art desktop pet.
```

## 5.4 后处理

每次生成后必须做后处理，不能直接把模型输出交给客户端。

后处理内容：

1. 透明背景处理。
2. 裁剪到统一画布。
3. 缩放到目标尺寸。
4. 统一像素网格。
5. 限制颜色数量。
6. 检查每帧尺寸一致。
7. 检查是否缺帧。
8. 输出 PNG Sprite Sheet。
9. 输出 GIF 预览。

## 6. GIF 预览需求

### 6.1 预览内容

网页端至少展示：

- 当前宠物静态预览。
- `idle.gif`
- `walk_right.gif`
- `sleep.gif`
- `alert.gif`
- `play.gif`

`walk_left` 可以用 `walk_right` 镜像生成，也可以单独展示。

### 6.2 GIF 生成

后端生成 GIF：

```text
Sprite Sheet
  -> 拆分帧
  -> 按 fps 设置 duration
  -> 合成 GIF
  -> 返回预览 URL
```

推荐：

- `idle`：4 fps。
- `walk`：8 fps。
- `sleep`：2-4 fps。
- `alert`：4 fps。
- `play`：8 fps。

GIF 只用于网页预览；正式资产包仍以 PNG Sprite Sheet 为主。这样客户端播放更可控。

## 7. 导出资产包

导出 `.deskpet` 文件，结构如下：

```text
pet-name.deskpet
  manifest.json
  preview.png
  previews/
    idle.gif
    walk_right.gif
    sleep.gif
    alert.gif
    play.gif
  sprites/
    idle.png
    walk_left.png
    walk_right.png
    sleep.png
    alert.png
    play.png
```

`manifest.json` 需要记录：

- 宠物名称。
- 物种。
- 风格。
- 画布尺寸。
- 每个动作的文件路径、帧数、FPS、帧宽高。
- 生成器版本。
- 资产格式版本。

## 8. MVP 功能范围

### Must Have

1. 上传 1-5 张宠物图片。
2. 选择猫/狗。
3. 自动生成宠物特征描述。
4. 用户可编辑关键特征。
5. 生成 4 个基础形象候选。
6. 用户选择一个候选。
7. 生成六组动作帧。
8. 生成 GIF 预览。
9. 导出 `.deskpet` 文件。

### Should Have

1. 生成失败重试。
2. 单个动作重新生成。
3. 手动选择“更像第几张照片”。
4. 简单调色和花纹强化。
5. 历史作品保存。

### Not In MVP

1. 完整像素画编辑器。
2. 社区分享市场。
3. 多宠物合照生成。
4. 复杂配饰系统。
5. 账号付费体系。

## 9. 验收标准

### 9.1 生成质量

1. 用户能识别宠物类型。
2. 主毛色和核心花纹保留。
3. 六组动作看起来像同一只宠物。
4. 动画无明显跳帧、错位和缺帧。
5. 透明背景干净。

### 9.2 资产质量

1. 所有 Sprite Sheet 尺寸符合 manifest。
2. 所有动作帧数符合定义。
3. GIF 能正常播放。
4. `.deskpet` 能被客户端导入。
5. 导入后桌宠播放不变形、不闪烁。

## 10. 主要风险

### 10.1 宠物一致性不足

应对：

- 先生成基础形象，再基于基础形象生成动作。
- 使用动作模板。
- 用户先选中最像的一版，再进入动作生成。
- 对单个动作支持重生成。

### 10.2 多帧动画不自然

应对：

- 先限制动作幅度。
- walk 使用模板动画。
- idle/sleep/play 用少量帧表达。
- 后续再增加更复杂动作。

### 10.3 生成成本高

应对：

- 先生成静态候选，再生成动作。
- 用户确认后再生成完整动作包。
- walk_left 默认由 walk_right 镜像得到。
- GIF 从 Sprite Sheet 派生，不额外调用模型。

## 11. 推荐开发顺序

1. 做一个固定样例 `.deskpet` 资产包。
2. 做网页端上传和任务状态页。
3. 做基础形象生成。
4. 做 GIF 预览。
5. 做 `.deskpet` 打包下载。
6. 再做六组动作帧生成。
7. 最后优化特征识别和一致性。

