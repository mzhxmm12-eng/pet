# Web 宠物资产生成平台 PRD

## 1. 背景

当前项目已有两部分能力：

- Windows 桌宠客户端：可扫描 `assets/pets/{pet-id}` 下的标准宠物资产，并在控制面板中选择、应用新猫咪。
- Web 生成工作台：已支持上传宠物照片、调用 image2 类接口生成基础候选图，但导出仍是占位内容，尚不能生成可直接导入客户端的完整资产包。

本 PRD 目标是将 Web 工作台升级为可商业化的宠物资产生成平台：用户通过卡密获得生成次数，上传宠物照片后生成 `.deskpet` 资产包，下载后自行导入桌宠客户端。

## 2. 产品目标

用户可以完成以下闭环：

```text
购买/兑换卡密
-> 获得生成次数
-> 上传宠物照片
-> 生成完整桌宠资产包
-> 下载 .deskpet
-> 在 Windows 客户端导入
-> 控制面板选择并应用新宠物
```

首期只要求猫咪完整可用，狗狗能力可保留入口但不作为必须完成项。

## 3. MVP 范围

### 必须实现

- Web 端卡密兑换。
- Web 端展示剩余生成次数。
- 上传 1-5 张猫咪照片。
- 填写宠物名称和特征补充。
- 调用 image2 接口生成基础候选。
- 基于选中候选生成完整动作资产。
- 后处理为客户端可用的标准 PNG sprite sheet。
- 导出 `.deskpet` 资产包。
- 桌面端支持导入 `.deskpet` 并添加到猫窝。
- 生成成功扣 1 次，失败不扣或自动返还。

### 暂不实现

- 在线支付自动发卡。
- 用户作品社区。
- 云端资产库公开分享。
- 内置像素编辑器。
- 多端同步。
- 复杂会员订阅。

## 4. 用户流程

### 4.1 兑换次数

```text
用户进入 Web 平台
-> 输入卡密
-> 服务端校验卡密
-> 成功后增加用户可用次数
-> 页面展示剩余次数
```

MVP 可采用轻账号或无账号模式：

- 推荐正式版：登录账户后兑换卡密，次数绑定账户。
- 内测版可简化：浏览器保存兑换后的 token，但服务端仍需记录卡密消耗。

### 4.2 生成宠物

```text
上传照片
-> 填宠物名和特征
-> 检查剩余次数
-> 生成基础候选
-> 用户选择候选
-> 生成动作源图
-> 后处理切图
-> 自动校验
-> 打包 .deskpet
-> 扣除 1 次
-> 用户下载
```

### 4.3 桌面端导入

```text
打开控制面板
-> 点击「添加猫咪」
-> 选择 .deskpet 文件
-> 客户端解包并校验
-> 复制到 assets/pets/{pet-id}
-> 刷新猫窝列表
-> 选中新猫咪
-> 点击「应用设置」
```

## 5. 资产包规范

`.deskpet` 本质为 zip 包，内部结构固定：

```text
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
metadata/
  source.json
  generation-report.json
```

### 5.1 manifest.json

必须兼容当前客户端 `PetAssetLoader`：

```json
{
  "formatVersion": 1,
  "petId": "user-cat-xxxx",
  "name": "猫咪名称",
  "species": "cat",
  "style": "pixel_cute",
  "author": "DeskPet Web",
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
  "animations": {}
}
```

`animations` 字段必须包含：

| 动作 | 文件 | 尺寸 | 帧数 | FPS |
|---|---|---:|---:|---:|
| idle | sprites/idle.png | 256x64 | 4 | 4 |
| walk_right | sprites/walk_right.png | 384x64 | 6 | 8 |
| walk_left | sprites/walk_left.png | 384x64 | 6 | 8 |
| sleep | sprites/sleep.png | 256x64 | 4 | 3 |
| play | sprites/play.png | 384x64 | 6 | 10 |
| play_left | sprites/play_left.png | 384x64 | 6 | 10 |
| eat | sprites/eat.png | 384x64 | 6 | 8 |
| reject | sprites/reject.png | 192x64 | 3 | 5 |
| dragged | sprites/dragged.png | 128x64 | 2 | 4 |

## 6. Web 端功能需求

### 6.1 上传与参数

页面需支持：

- 上传 JPG / PNG / WebP。
- 最多 5 张。
- 宠物名称输入。
- 特征补充输入。
- 显示剩余生成次数。
- 开始生成按钮。
- Mock 预览能力可保留，但正式导出必须区分真实生成和 Mock。

### 6.2 基础候选生成

后端接口：

```text
POST /api/pet-generation/base
```

输入：

- 图片文件列表。
- petName。
- species。
- feature note。

输出：

- jobId。
- 2-4 个基础候选。
- 每个候选包含 imageUrl、description、likenessScore。

要求：

- 候选图必须是像素风全身宠物。
- 候选图必须保留用户填写的关键特征。
- 用户必须选择一个候选后才能生成动作。

### 6.3 动作生成

后端接口：

```text
POST /api/pet-generation/actions
```

输入：

- jobId。
- candidateId。

输出：

- 动作源图 URL 或处理后的动作预览。
- 生成状态。

推荐生成策略：

```text
第一张源图：preview + idle + walk_right
第二张源图：sleep + play + eat + reject + dragged
```

`walk_left` 必须由 `walk_right` 水平翻转生成。  
`play_left` 必须由 `play` 水平翻转生成。

### 6.4 后处理与打包

后端接口：

```text
POST /api/pet-generation/package
GET /api/pet-generation/jobs/:jobId/download
```

后处理流程：

```text
AI 源图
-> 去除 chroma key 背景
-> 按连通主体蒙版提取每帧
-> 缩放到 64x64
-> 保持 2-3px 安全边距
-> 合成 sprite sheet
-> 生成左右翻转动作
-> 写 manifest.json
-> 写 generation-report.json
-> 打包为 .deskpet
```

禁止仅用矩形裁切相邻帧；必须使用连通主体蒙版，避免带入脸、尾巴、脚等残片。

## 7. 生成质量要求

### 7.1 通用要求

- 所有 PNG 必须透明背景。
- 每帧必须 64x64。
- 猫咪完整保留：耳朵、尾巴尖、身体、腿、脚掌不能裁切。
- 每帧主体距离边缘至少 2px，推荐 3px。
- 不允许出现 UI、文字、箭头、鼠标、目标点、辅助线。
- 眼睛必须干净完整，不能糊成黑块、断裂像素或脏点。
- 同一资产包内必须像同一只猫。

### 7.2 走路要求

`walk_right/walk_left` 是最高风险动作，必须重点校验：

- 必须是真四足步态，不是整只猫平移。
- 前脚前伸时，对侧后脚后蹬。
- 应包含落脚、支撑、摆动、互换阶段。
- 至少 5/6 帧有有效脚步相位变化。
- 脚掌必须相对身体发生前后变化。
- 不允许幽灵脚、重复脚影、断腿、细线腿、漂浮脚掌。
- 不允许前方或后方出现小残片。

### 7.3 睡觉要求

- 4 帧必须保持同一朝向。
- 只允许轻微呼吸、眨眼、耳朵或尾巴变化。
- 不允许突然切到背面、反方向、坐姿或站姿。

### 7.4 Play 要求

- 必须是斜上跳跃动作。
- 需要包含伏低、起跳、最高点、落地、恢复。
- 最高点不能裁切耳朵、爪子或尾巴。
- 不允许水平滑动假动作。

## 8. 质量校验输出

每次打包必须生成：

```text
metadata/generation-report.json
```

示例：

```json
{
  "passed": true,
  "warnings": [],
  "checks": {
    "manifest": "passed",
    "dimensions": "passed",
    "alpha": "passed",
    "framePadding": "passed",
    "walkGait": "passed",
    "sleepStability": "passed",
    "playArc": "passed"
  }
}
```

若校验失败：

- 不允许扣除最终次数。
- 不允许提供正式下载。
- 页面展示失败原因。
- 可自动重试一次或允许用户局部重生。

## 9. 卡密与次数系统

### 9.1 规则

- 用户通过卡密获得生成次数。
- 完整生成 1 个可下载 `.deskpet` 包消耗 1 次。
- API 失败不扣次数。
- 后处理失败不扣次数。
- 打包失败不扣次数。
- 用户主动重新生成完整资产包，消耗新次数。

MVP 可简化为：生成开始先冻结或扣除 1 次，失败后返还。

### 9.2 数据模型

建议表结构：

```text
users
- id
- email / phone / openid
- credit_balance
- created_at

redeem_codes
- id
- code_hash
- total_credits
- status: unused / redeemed / disabled
- batch_id
- redeemed_by_user_id
- redeemed_at
- created_at

generation_jobs
- id
- user_id
- pet_id
- pet_name
- status: pending / generating / packaging / completed / failed
- credit_charged
- output_package_url
- error_reason
- created_at
- completed_at

credit_ledger
- id
- user_id
- type: redeem / reserve / consume / refund / admin_adjust
- amount
- job_id
- code_id
- created_at
```

必须使用 `credit_ledger` 记录流水，不要只修改用户余额。

### 9.3 卡密安全

- 卡密格式建议：`DP-XXXX-XXXX-XXXX`。
- 数据库只存 hash，不存明文。
- 同一卡密只能兑换一次。
- 支持批次禁用。
- 支持查询兑换和消耗记录。

## 10. 桌面端导入需求

控制面板「添加猫咪」需要升级：

- 当前占位卡片点击后打开文件选择器。
- 支持选择 `.deskpet`。
- 解压到临时目录。
- 校验 manifest 和 PNG 文件。
- 若 `petId` 冲突，提示覆盖或自动生成新 ID。
- 复制到客户端可扫描目录。
- 刷新猫窝列表。
- 自动选中新导入猫咪。

导入失败时需展示明确原因：

- manifest 缺失。
- 图片尺寸不匹配。
- 动作缺失。
- petId 冲突。
- zip 损坏。

## 11. 前端页面改造

建议改成 4 步向导：

1. 上传照片。
2. 确认宠物特征。
3. 选择基础形象。
4. 预览动作并下载资产包。

结果页必须展示：

- 预览图。
- 待机。
- 走路。
- 睡觉。
- 玩耍。
- 吃东西。
- 拒绝。
- 拖拽。
- 生成报告。
- 下载按钮。

后续可增加「重生某个动作」按钮，但 MVP 可先不做。

## 12. 验收标准

### Web 端

- 用户可兑换卡密并看到剩余次数。
- 用户可上传照片并生成基础候选。
- 用户可选择候选并生成完整动作。
- 生成成功后可下载 `.deskpet`。
- 失败不扣次数或自动返还。
- 下载包内文件结构符合规范。

### 客户端

- 可选择 `.deskpet` 导入。
- 导入后猫窝出现新猫。
- 点击应用后桌面显示新猫。
- 所有动作可正常加载。
- 导入失败时有明确错误提示。

### 生成质量

- `walk_right/walk_left` 走路脚步有合理四足交替。
- `sleep` 不跳方向。
- `play` 有斜上跳跃。
- 所有帧无裁切、无残片、无坏眼睛。

## 13. 开发顺序建议

1. 定义 `.deskpet` zip 规范和导入校验逻辑。
2. 桌面端实现 `.deskpet` 导入。
3. Web 端实现真实 package/export，不再返回占位 manifest。
4. 后端实现图片后处理：去背景、连通主体切帧、合成 sprite sheet。
5. 接入卡密和次数系统。
6. 前端补齐兑换、剩余次数、生成报告、下载体验。
7. 增加局部重生和历史记录。

