# 图书导入导出需求

## 需求

### R1 图书目录导出
- WHEN 管理员在 Library Admin 页面触发导出命令，THE 系统 SHALL 生成包含当前筛选图书目录的 CSV 文本。
- THE CSV SHALL 包含表头：ISBN、Title、Author、Category、Location、PublishedYear、CopyCount、Rating。

### R2 图书目录导入
- WHEN 管理员提交 CSV 文本，THE 系统 SHALL 逐行解析图书数据并按 ISBN 执行新增或更新。
- IF CSV 行缺少标题或作者，THE 系统 SHALL 跳过该行并在结果提示中统计跳过数量。
- IF CSV 中的 CopyCount 小于 1，THE 系统 SHALL 按 1 处理。

### R3 MVVM 页面交互
- WHEN 导入或导出完成，THE 系统 SHALL 通过现有 MVVM state 回填结果提示和 CSV 文本。
- THE 功能 SHALL 使用现有 `data-cs-vm`、`data-cs-bind`、`data-cs-command` 交互模式。
