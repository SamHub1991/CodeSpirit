# 图书导入导出设计

## 方案

- 在 `LibraryService` 中提供 `ExportBooksCsv(query, status, category)` 和 `ImportBooksCsv(csv)`。
- CSV 导出复用现有筛选逻辑，输出当前筛选后的图书字段。
- CSV 导入使用简单 CSV 解析器，支持双引号转义与逗号字段。
- ISBN 作为幂等键：匹配已有 ISBN 时更新图书，未匹配时新增图书。
- 在 `AdminViewModel` 增加 `ImportExportCsv` 双向绑定字段，以及 `ExportBooks`、`ImportBooks`、`ClearImportExport` 命令。
- 在 `Admin.aspx` 增加导入导出卡片，使用 textarea 承载 CSV 文本。
- 同步更新 VSIX 模板副本，保持生成项目功能一致。
