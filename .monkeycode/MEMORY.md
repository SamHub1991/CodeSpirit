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
