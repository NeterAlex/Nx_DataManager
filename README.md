# Nx DataManager

> 项目仍在开发中

## 项目简介

Nx DataManager 是一款文件备份管理软件，提供全面的数据备份功能，支持多种备份策略、远程存储、版本控制和备份调度等功能。基于 WPF 框架、MVVM架构和 .NET 10 平台构建。

![image](https://github.com/NeterAlex/Nx_DataManager/blob/master/static/preview.png)

## 核心功能

### 备份管理

#### 多种备份类型
- **全量备份** - 完整备份所有源文件
- **增量备份** - 仅备份自上次备份后变化的文件
- **差异备份** - 备份自上次全量备份后变化的文件

#### 高级备份特性
- 文件压缩（支持多种压缩级别）
- AES 加密保护
- 断点续传
- 带宽限制控制
- 智能去重检测
- VSS（卷影复制服务）支持
- 备份预览与差异分析

### 调度系统


- 手动执行备份
- 每日/周/月定时执行备份
- 自定义间隔执行备份
- 计划任务执行历史追踪
- 任务状态监控
- 失败重试机制

### 版本控制

- 文件版本历史管理
- 版本比较与恢复
- 可配置的版本保留策略、旧版本自动清理

### 远程存储支持

#### SMB/CIFS 协议
- Windows 网络共享连接
- 域认证支持
- 加密传输选项

#### WebDAV 协议
- 云存储服务对接
- SSL/TLS 加密支持
- 跨平台访问能力

### 数据分析与报告

- 备份成功率趋势分析
- 存储空间使用统计
- 备份性能指标监控
- 备份任务健康评估
- CSV、HTML 格式导出

## 主要技术栈
### 库与框架

- WPF on .NET 10（框架与平台）
- CommunityToolkit.Mvvm（MVVM框架）
- Xaml.Behaviors（WPF Behavior 支持）
- SQLite（数据库存储）
- Dapper（ORM）
- Extensions.DependencyInjection（依赖注入容器）

### 核心服务

```
IBackupService          - 备份核心服务
IStorageService         - 数据持久化服务
ISchedulerService       - 任务调度服务
ICompressionService     - 压缩服务
IEncryptionService      - 加密服务
IVersionControlService  - 版本控制服务
IBandwidthLimiter       - 带宽限制服务
INotificationService    - 通知服务
IReportExportService    - 报告导出服务
```


## 开发指南

### 环境要求

- **操作系统** - Windows 11 (64-bit)
- **.NET SDK** - .NET 10.0 或更高版本
- **开发工具** - Visual Studio 2022 17.12+ 或 Rider 2024.3+

### 构建项目

```bash
# 克隆仓库
git clone https://github.com/NeterAlex/Nx_DataManager.git
cd Nx_DataManager/NxDataManager

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行应用
dotnet run
```

### 项目结构

```
NxDataManager/
├── Controls/           # 自定义控件
├── Converters/         # 值转换器
├── Data/              # 数据访问层
│   └── Repositories/  # 数据仓储
├── Helpers/           # 辅助工具类
├── Models/            # 数据模型
├── Services/          # 业务服务
├── ViewModels/        # 视图模型
├── Views/             # 视图界面
├── Styles/            # 样式资源
└── Animations/        # 动画效果
```

## 许可协议

本项目采用 MIT 许可协议。详见 [LICENSE](LICENSE) 文件。

