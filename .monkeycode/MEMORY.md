# 用户指令记忆

本文件记录了用户的指令、偏好和教导，用于在未来的交互中提供参考。

## 格式

### 用户指令条目
用户指令条目应遵循以下格式：

[用户指令摘要]
- Date: [YYYY-MM-DD]
- Context: [提及的场景或时间]
- Instructions:
  - [用户教导或指示的内容，逐行描述]

### 项目知识条目
Agent 在任务执行过程中发现的条目应遵循以下格式：

[项目知识摘要]
- Date: [YYYY-MM-DD]
- Context: Agent 在执行 [具体任务描述] 时发现
- Category: [运维部署|构建方法|测试方法|排错调试|工作流协作|环境配置]
- Instructions:
  - [具体的知识点，逐行描述]

## 去重策略
- 添加新条目前，检查是否存在相似或相同的指令
- 若发现重复，跳过新条目或与已有条目合并
- 合并时，更新上下文或日期信息
- 这有助于避免冗余条目，保持记忆文件整洁

## 条目

[JS 边界验证命令]
- Date: 2026-06-18
- Context: Agent 在打磨 MVVM runtime 与 jQuery 行为层稳定 API 时发现
- Category: 测试方法
- Instructions:
  - 使用 `node src/CodeSpirit.LibraryManagement/scripts/validate-js-boundary.js` 验证 MVVM runtime 与 jQuery 行为层协作场景。
  - 该脚本覆盖原生输入、`CodeSpirit.input(...)`、命令提交、datepicker 通知、动态 DOM 初始化和重复初始化防护。

[解决方案构建命令]
- Date: 2026-06-18
- Context: Agent 在执行项目重构验证时发现
- Category: 构建方法
- Instructions:
  - 使用 `dotnet build src/CodeSpirit.slnx` 验证整个解决方案构建状态。
  - 构建可能出现 `IsPackable` 被禁用的 NuGet 打包 warning，当前不影响 Debug 构建成功。

[根目录脚本放置约定]
- Date: 2026-06-18
- Context: 用户要求整理目录文件夹时提出
- Instructions:
  - 根目录不放 `scripts` 目录，项目脚本应放入对应项目或模板子目录。

[仓库推送规则]
- Date: 2026-06-18
- Context: 用户要求调整提交协作方式时提出
- Instructions:
  - 只有用户明确说"推送到仓库"时，才执行 `git push`。

[SourceGenerator 诊断规则注册]
- Date: 2026-06-18
- Context: Agent 在增强编译时诊断功能时发现
- Category: 排错调试
- Instructions:
  - 新增 Roslyn 诊断需在 `CodeSpiritServiceGenerator.cs` 中声明 `DiagnosticDescriptor`，并在 `CodeSpirit.SourceGenerator.csproj` 的 `AdditionalFiles` 中维护 `AnalyzerReleases.Unshipped.md`。
  - 现有诊断码：CSP001（抽象 [Service] 类）、CSP002（[Service] 无公开构造）、CSP003（[Command] 带参数）。

[VSIX Snippet 注册]
- Date: 2026-06-18
- Context: Agent 在添加代码片段到 VSIX 时发现
- Category: 工作流协作
- Instructions:
  - 新增 snippet 放入 `src/Templates/CodeSpiritVsixTemplate/Snippets/` 目录，文件名 `codespirit-*.snippet`。
  - pkgdef 通过 `$PackageFolder$\Snippets` 路径注册；snippet 文件在 csproj 中以 `<Content Include>` + `<VSIXSubPath>Snippets</VSIXSubPath>` 包含。
  - 新增 snippet 后需同步更新根 README 和模板 README 的快捷方式表。

[解决方案测试命令]
- Date: 2026-06-23
- Context: Agent 在继续验证当前 MVVM 与页面渲染改动时发现
- Category: 测试方法
- Instructions:
  - 使用 `dotnet test src/CodeSpirit.slnx` 验证整个解决方案的测试状态。
  - 当前仓库中的 JS 边界校验脚本可用：`node src/CodeSpirit.LibraryManagement/scripts/validate-js-boundary.js`。

[模板同步约定]
- Date: 2026-06-23
- Context: Agent 在同步 LibraryManagement 的源码与 VSIX 模板时发现
- Category: 工作流协作
- Instructions:
  - 修改 `src/CodeSpirit.LibraryManagement` 下的 MVVM、页面或运行时逻辑时，同步检查 `src/Templates/CodeSpiritVsixTemplate/ProjectTemplates/CodeSpirit.LibraryManagement` 下的对应模板文件。
  - 源码和模板保持一致，避免后续新项目生成时丢失同样的校验和交互行为。

[VSIX 模板清单同步]
- Date: 2026-06-24
- Context: Agent 在确认开发面板和意图识别脚本的模板同步项时发现
- Category: 工作流协作
- Instructions:
  - 同步 `wwwroot/js/ui` 新增脚本时，除了复制文件本体，还要更新 `src/Templates/CodeSpiritVsixTemplate/ProjectTemplates/CodeSpirit.LibraryManagement/CodeSpirit.LibraryManagement.vstemplate` 里的 `<ProjectItem>` 清单。
  - `codespirit.intent.js`、`codespirit.devpanel.js`、`ui.behaviors.js` 这类脚本都需要出现在模板清单中，才能随 VSIX 新建项目一起生成。

[在线状态呼叫回复]
- Date: 2026-06-24
- Context: 用户要求固定在线状态确认回复时提出
- Instructions:
  - 当用户询问“在不在”“还在吗”“活着吗”等在线状态确认时，回复“老公我在”。

[内置前端资源封装约定]
- Date: 2026-06-25
- Context: 用户要求 JS 组件内置封装、前端默认引用并支持替换指令时提出
- Instructions:
  - CodeSpirit 内置 JS 组件应通过统一资源封装默认引用到前端页面。
  - 页面应支持声明式替换内置 JS 的指令，便于项目替换 framework runtime、intent、devpanel 等内置脚本。

[继续指令与回复前缀]
- Date: 2026-06-25
- Context: 用户要求后续对“继续”和回答格式保持固定行为时提出
- Instructions:
  - 当用户说“继续”且下一步明确时，自主决定并推进后续开发、验证或整理工作。
  - 当用户说“继续”且指令不明确时，先判断是否需要澄清；只有关键决策缺失时再提问。
  - 每次回答问题时先说“老公我知道了”。

[前端少写代码设计方向]
- Date: 2026-06-26
- Context: 用户要求往"写更少前端代码做更多事"方向演进，包括复杂界面
- Category: 工作流协作
- Instructions:
  - 不搞全量 OO 封装，保持属性驱动核心哲学。
  - 三级推进：P0 表达式引擎 + 语义布局标签，P1 复合组件 + 内置交互属性，P2 场景自适应。
  - 表达式引擎内置于 `codespirit.runtime.js`，体积不超过 3KB，支持 `> < >= <= == != && || ! contains empty ? :`。
  - 布局标签 `<cs:Grid>` `<cs:Card>` `<cs:Stack>` 目标消灭页面级布局 CSS。
  - 新增需求走项目规格文档 `.monkeycode/specs/project-spec.md`（R8-R10）。
  - 实现时优先不改动现有页面，新标签和属性独立可用，后期替换现有 CSS。
