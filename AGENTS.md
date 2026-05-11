# DeskPet Agent Handoff

## 当前项目状态

这是一个 Windows 桌面宠物项目，当前重点分成两块：

- `web/`：网页端宠物形象生成器，已实现 Next.js 工作台和真实生图 API 接入。
- `DeskPet.App/`：Windows 桌宠客户端，当前工作区已有未提交改动，接手前先查看 `git status`，不要随意回滚。

## 网页端已完成

`web/` 使用 `Next.js 16 + React 19 + TypeScript + Tailwind CSS + lucide-react`。

已完成能力：

- 上传猫/狗图片，最多 5 张。
- 填写宠物名称和一个“特征补充”字段。
- 调用服务端 Route Handler，避免 API Key 暴露到浏览器。
- 通过 `gpt-image-2` 接入真实图生图，生成基础宠物候选图。
- 提供 Mock 预览按钮，方便无 API 或 API 失败时看 UI 流程。
- 预留动作生成和 `.deskpet` manifest 导出接口。

关键文件：

- `web/src/app/page.tsx`：生成工作台 UI。
- `web/src/lib/pet-generation-api.ts`：前端 API adapter 和 Mock 流程。
- `web/src/lib/server/image-provider.ts`：服务端生图 provider。
- `web/src/app/api/pet-generation/*/route.ts`：Next Route Handlers。
- `web/src/types/pet-generation.ts`：核心类型。

## API 配置

真实 API 配置在 `web/.env.local`，该文件被 git ignore，不要提交。

当前可用配置方向：

```env
IMAGE_API_BASE_URL=https://api.glmbigmodel.me
IMAGE_API_MODEL=gpt-image-2
IMAGE_API_MODE=edits
IMAGE_API_INCLUDE_MODEL=true
IMAGE_API_SIZE=1024x1024
```

注意：

- `gpt-image-2` 的 image edits 上传字段使用 `image`。
- `jimeng-*` 的 image edits 上传字段使用 `images`。
- 这两个分支已在 `image-provider.ts` 中兼容。

## 常用命令

在 `web/` 下：

```bash
npm run dev
npm run lint
npm run build
```

本地网页默认地址：

```text
http://127.0.0.1:3000
```

## 下一步建议

优先方向：

1. 给生成候选图加“下载 PNG”按钮。
2. 将真实生成图保存为 `.deskpet` 资产包内容。
3. 将动作预览从单张图升级为 Sprite Sheet / GIF 生成。
4. 再把网页导出的 `.deskpet` 与 Windows 客户端导入流程打通。

## 接手注意

- 根目录当前可能有未提交的桌面端和素材改动，先确认来源再修改。
- 不要把 `.env.local`、`.next/`、`node_modules/` 提交进 git。
- `web/AGENTS.md` 是 Next 脚手架规则，保留不动。
