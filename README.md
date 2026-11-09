# Pancake Loop / 煎饼循环

一款以制作煎饼果子为主题的 2D 叙事小游戏，最初完成于字节跳动 Game Jam，现已升级至 Unity 6，并针对制作流程、交互反馈、运行性能与 WebGL 游玩进行了优化。

## 在线游玩

GitHub Pages：<https://charles0122.github.io/pancake-loop/>

> 网页版首次打开需要下载并初始化约 18 MB 的 WebGL 内容（由原约 29 MB 优化而来）。首次加载后浏览器会缓存资源；切换到其他标签页时游戏会自动暂停，以降低功耗。

## 操作方式

- 鼠标拖动：将食材拖入锅中
- `B`：上餐
- `A`：下一位顾客
- `H`：打开说明
- `Esc`：暂停

## 主要优化

- 增加倒面糊、摊饼、加料、烹饪变色、蒸汽、折叠与上餐动画
- 增加订单卡片、按键提示、拖拽落点与失败反馈
- 接入煎制、鸡蛋、计时、上餐、金币与 UI 音效
- 增加清晰的主菜单“开始营业”入口
- 升级旧 Unity 2020 包至 Unity 6000.3 兼容版本
- WebGL 首次下载从约 29 MB 优化至约 18 MB：移除未使用音频、裁短烹饪音效、去重精灵、清理字体缓存，并启用 IL2CPP Optimize Size / LTO
- 限制 60 FPS、稳定画面失焦暂停、WebGL 低功耗 GPU 偏好，并移除 UI 轮询热路径
- 提供 GitHub Pages 可直接部署的 WebGL 构建

## 开发环境

- Unity `6000.3.13f1`
- Built-in Render Pipeline
- WebGL Build Support

## 本地开发

1. 使用 Unity Hub 安装 Unity `6000.3.13f1` 和 Web Build Support。
2. 在 Unity 中打开项目。
3. 打开 `Assets/Scenes/Persistent.unity` 并运行。

## WebGL 构建

项目包含 Unity 6 WebGL Build Profile：

```text
Assets/Settings/Build Profiles/WebGL.asset
```

发布成品位于 `docs/`。构建采用 Brotli 压缩并启用 Decompression Fallback，以兼容 GitHub Pages。

## License

项目美术、音频与代码版权归原作者所有。未经许可，请勿将项目素材用于商业用途。
