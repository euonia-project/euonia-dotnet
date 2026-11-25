# 权限文档图示

本目录存放「权限系统」相关文档的 SVG 图示，供 GitHub / GitLab 直接渲染。
图示内容与 `Source/Euonia.Security`、`Source/Euonia.Osba` 的当前实现保持一致。

## 目录结构

```text
docs/assets/permission/
├── README.md          # 本文件
├── render.py          # 批量渲染脚本（Kroki）
├── src/
│   ├── security-layering.mmd
│   └── ...            # 每张图一份 Mermaid 源
├── security-layering.svg
└── ...                # 与 src 同名的 SVG
```

## 命名

- 文件名即语义：`security-*` 属于 `Euonia.Security` 文档，`permission-*` 为总览图，
  `osba-*` / `core-*` 属于其他包的文档。
- 同一张图被多份文档引用时只保留一份 SVG（例如分层图同时用于包 README 与仓库根 README）。
- 图题编号由文档决定，文件名不带编号；改了图题不必重命名文件。
- `*-en` 表示英文标签版本，供英文文档使用。

## 当前图清单

| 文件 | 用于 | 主题 |
|---|---|---|
| `security-layering.svg` / `security-layering-en.svg` | 仓库根 README「模块总览」 | Core / Security / Osba 的依赖方向 |
| `permission-two-tracks.svg` / `permission-two-tracks-en.svg` | 仓库根 README「Security」节 | 权限码与授权标识两条判定线 |
| `security-assembly.svg` | 包 README §3 | 装配与契约关系 |
| `security-sequence.svg` | 包 README §3.2 | 权限码从声明到判定的时序 |
| `security-code-vs-identifier.svg` | 包 README §5.8 | 权限码与授权标识的分轨 |
| `security-query-vs-gate.svg` | 包 README §9 | 查询、显式码判定与闸门的范围 |
| `security-tutorial-sequence.svg` | 用户手册 TUTORIAL | 订单场景端到端时序 |
| `security-api-contracts.svg` | 用户手册 API | Core / Security / 宿主职责 |

`osba-*` 与 `core-*` 由其他包的文档维护。

## 生成

所有 SVG 由 [Kroki](https://kroki.io) 渲染，**必须带 `?html-labels=false`**：这样标签输出为
`<text>`/`<tspan>`，而不是 `<foreignObject>`（GitHub 不渲染 `foreignObject` 里的文字，
图会变成空白）。脚本已固定该参数并做校验。

```bash
# 全量渲染 src/ 下的所有 .mmd
python3 docs/assets/permission/render.py

# 只渲染一张（可写带或不带扩展名）
python3 docs/assets/permission/render.py security-layering.mmd
```

脚本只依赖 Python 标准库，需要访问 `https://kroki.io`。任一图渲染失败、或校验不通过时以非零码退出。

## 新增 / 更新一张图

1. 在 `src/` 下新建或修改 `<name>.mmd`（只写 Mermaid 源码，不要包含 ``` 围栏）。
2. 运行 `python3 docs/assets/permission/render.py <name>.mmd` 生成/覆盖 `<name>.svg`。
3. 在文档中用 HTML 或 Markdown 引用：

   ```html
   <img src="../../docs/assets/permission/<name>.svg" alt="图 N：图题" width="760" />
   ```

4. 保留图题与正文的「如图 N 所示」。路径按文档自身位置计算（包 README 用 `../../docs/assets/...`，
   用户手册用 `../../assets/...`，仓库根文档用 `docs/assets/...`）。
5. 提交时同时包含 `.mmd` 与 `.svg`，二者必须同步。

## 约定

- **流程图**在源文件首行用 `%%{init: {"flowchart": {"wrappingWidth": 420}}}%%` 设定换行宽度，
  避免 Kroki 默认宽度把长类名断在词中间。
- `render.py` 会在 SVG 根节点后插入一层白色底衬（`class="permission-backdrop"`）：Mermaid 默认透明背景 +
  深灰文字，在 GitHub 深色主题下不可读；白底在浅色主题下无视觉差异。
- 时序图较宽（约 1500px），文档里用 `width="900"` 引用；流程图用 `width="760"`。

## 校验

```bash
grep -c '<foreignObject' docs/assets/permission/*.svg   # 必须全为 0
grep -c '<text' docs/assets/permission/*.svg            # 必须大于 0
```
