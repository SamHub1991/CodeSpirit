# cs 元素标签增强任务清单

## 阶段 1：低风险样板收敛

- [ ] 增加 `cs:Form`，渲染为带 `data-cs-vm` 的标准 MVVM 表单。
- [ ] 增加 `cs:Button`，渲染为带 `data-cs-command` 的提交按钮。
- [ ] 在示例项目和 VSIX 模板中替换重复表单按钮写法。
- [ ] 更新项目 README，说明新标签用法。

## 阶段 2：字段组件

- [ ] 增加 `cs:Field`，支持 `Name`、`Label`、`Placeholder`、`Type`、`Rows`。
- [ ] 支持 input 与 textarea 自动生成。
- [ ] 支持基于绑定名生成 `name` 和 `data-cs-bind`。
- [ ] 在 Admin 页面逐步替换高重复 label/input。

## 阶段 3：列表组件

- [ ] 增加 `cs:Table` 基础标签。
- [ ] 支持列定义、空状态和集合绑定。
- [ ] 支持常用格式化输出。
- [ ] 用 `cs:Table` 重构图书、读者、借阅、预约和库存审计表格。

## 阶段 4：局部刷新区域

- [ ] 增加 `cs:Region`，标记可局部刷新的页面区域。
- [ ] 扩展命令响应协议，支持 region state 或 HTML patch。
- [ ] 在 Admin 页面统计卡片、通知区域和表格区域试点。

## 阶段 5：验证与文档

- [ ] 为新增标签补充渲染测试或轻量验证脚本。
- [ ] 更新模板 README 和根 README 的标签能力说明。
- [ ] 保持 VSIX 模板与示例项目同步。
