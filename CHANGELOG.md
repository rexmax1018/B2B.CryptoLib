# Changelog（變更記錄）

## 2.0.1

### Changed（變更）

- 將正式環境直接相依套件更新為 `Autofac` 9.3.2、`Newtonsoft.Json` 13.0.4
  與 `BouncyCastle.Cryptography` 2.7.0。
- 以 `BouncyCastle.Cryptography` 取代已淘汰的 `Portable.BouncyCastle` 套件
  識別名稱；既有的 Bouncy Castle 命名空間與執行階段密碼路徑維持使用。
- 將測試專案遷移至 xUnit v3 4.0.0，搭配 `Microsoft.NET.Test.Sdk` 18.9.0、
  `xunit.runner.visualstudio` 4.0.0 與 .NET 10 Microsoft Testing Platform 執行器
  設定。
- 為可重用的執行階段與 KeyGeneration 組件啟用 XML 文件輸出。
- 新增架構、金鑰管理、密碼相容性與離線封裝指南。

### Compatibility（相容性）

- 公開執行階段程式碼的使用方式仍與 `Crypto`、`CryptoClient`、
  `IDataEncryptionService`、`ICryptoService` 及 `CryptoSuiteModule` 相容。
- 密文封裝格式仍為 `Base64(payload).unifiedName`。
- GCM v2 保留 `B2BCGCM` 標記、版本 2、隨機 12 位元組 nonce（隨機數）、統一名稱的
  UTF-8 AAD 與 16 位元組訊息驗證標籤。
- 沒有 GCM 標記的舊版 AES-CBC／PKCS#7 載荷仍可由讀取器讀取。
- 目前與舊版金鑰配置、RSA OAEP、舊版 RSA PKCS#1 v1.5 材料、ECC
  PEM／簽章行為、金鑰輪替與快取失效行為都維持不變。

## 2.0.0

2.0.0 條目只反映儲存庫歷史中導向已合併 PR #1 的實際變更：
`fce942e`（`feat: modernize CryptoLib runtime and usability`）、`2ee7f2e`
（`fix: harden CryptoLib v2 runtime contracts`），以及屬於已合併實作的較早
`5be1226`（`feat: support legacy key set compatibility`）。

### Changed（變更）

- 將方案的執行階段、金鑰產生、工具與測試專案移至 SDK-style .NET 10，
  並啟用 nullable reference types（可為 null 參考型別）與明確的專案邊界。
- 新增程序層級 `Crypto` 外觀介面與隔離的 `CryptoClient`，提供明確的執行階段
  選項與啟用統一名稱語意。
- 透過 `CryptoSuiteModule` 保留 Autofac 的可選整合介面，同時將離線產生流程
  分離到 `KeyGenerationModule` 與 `KeyGenTool`。
- 新增 `.der`、`.public.pem` 與 `.private.pem` 的舊版金鑰組相容性、舊版
  RSA PKCS#1 v1.5 AES 材料包裝，以及歷史密文使用的 AES-CBC 回退路徑。
- 新增執行階段使用的 current／history／update 金鑰組生命週期、輪替與快取失效。

### Compatibility（相容性）

- 執行階段加密與解密使用明確的統一名稱契約；解密從密文尾綴解析金鑰。
- 執行階段建構不依賴舊版靜態 `CryptoConfig`，離線產生器則保留這個
  設定 API。

## Upgrading from 2.0.0 to 2.0.1（從 2.0.0 升級至 2.0.1）

既有公開執行階段介面不需要修改應用程式碼：

```text
Crypto
CryptoClient
IDataEncryptionService
ICryptoService
CryptoSuiteModule
```

主要相依套件圖變更如下：

```text
Autofac 6.0.0 -> 9.3.2
Newtonsoft.Json 13.0.1 -> 13.0.4
Portable.BouncyCastle 1.9.0
    -> BouncyCastle.Cryptography 2.7.0
```

若應用程式直接鎖定 Autofac，請確認宿主最終解析的版本與 CryptoLib 2.0.1
相容。若應用程式直接使用舊的 Portable.BouncyCastle 套件，請把該直接引用
遷移到新的套件識別名稱；只有在應用程式透過 CryptoLib 使用功能時，既有的
`Org.BouncyCastle` 程式碼命名空間不需要進行應用程式層級 API 重寫。

下游 `B2B_API` 候選版本需要另外將 `B2B.CryptoLib` 從 2.0.0 更新到 2.0.1，並將
Autofac 從 9.1.0 更新到 9.3.2。該下游工作刻意不包含在這次僅限 CryptoLib 的
變更中，本次沒有修改任何 `B2B_API` 檔案。
