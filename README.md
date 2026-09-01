# B2B.CryptoLib

## Overview（概覽）

B2B.CryptoLib 2.0.1 是僅支援 .NET 10 的 AES、RSA、ECC 與以金鑰組為基礎的
執行階段文字加密函式庫。高階執行階段 API 不要求使用依賴注入；Autofac
只是可選的整合方式。

執行階段使用明確的金鑰執行脈絡。新的文字密文每次都使用新的 GCM nonce（隨機數）
而產生隨機結果；解密則依密文尾端的 `unifiedName` 選擇金鑰。金鑰材料絕對
不可提交到此儲存庫。

## Installation（安裝）

從指定的套件來源引用候選套件：

```xml
<PackageReference Include="B2B.CryptoLib" Version="2.0.1" />
```

套件只支援 `net10.0`，並宣告執行階段相依套件 `Autofac` 9.3.2、
`BouncyCastle.Cryptography` 2.7.0 與 `Newtonsoft.Json` 13.0.4。

## Quick Start（快速開始）

以明確且受保護的金鑰組根目錄與啟用的統一名稱，初始化程序內的預設用戶端
一次：

```csharp
using B2B.CryptoLib;
using B2B.CryptoLib.Models;

Crypto.Initialize(new CryptoOptions
{
    KeyManagerBasePath = @"D:\B2B\Keys",
    ActiveUnifiedName = "B2B_20260901"
});

var encrypted = Crypto.Encrypt("test");
var plainText = Crypto.Decrypt(encrypted);
```

`Crypto.Initialize` 具備執行緒安全保證。使用相同正規化設定重複呼叫是冪等
操作；不同設定會被拒絕。初始化前使用外觀介面會快速失敗。外觀介面不會讀取
`appsettings.json`、建立預設的 `Keys` 目錄、依名稱排序選取最新金鑰，或消費
`update` 檔案。

金鑰發布必須明確執行：

```csharp
await Crypto.UpdateKeySetsAsync();
```

## Isolated Client（隔離的 Client）

當同一程序需要多個獨立金鑰根目錄或金鑰執行脈絡時，請使用 `CryptoClient`：

```csharp
var client = CryptoClient.Create(new CryptoOptions
{
    KeyManagerBasePath = @"D:\B2B\TenantA\Keys",
    ActiveUnifiedName = "tenant-a-key"
});

var encrypted = client.Encrypt("value");
var plainText = client.Decrypt(encrypted);
```

每個用戶端擁有自己的金鑰管理器、快取、目錄執行脈絡與可選的啟用名稱。
建構時會建立 `current`、`history` 與 `update`，但不會發布或消費待處理檔案。
省略 `ActiveUnifiedName` 時，請使用 `client.Encrypt(value, unifiedName)`。
解密一律使用密文尾綴中的名稱。共用同一根目錄的不同用戶端不會共用快取或
更新鎖；請在外部協調輪替，或每個根目錄只使用一個用戶端。

## Optional Autofac（可選的 Autofac）

既有的 Autofac 應用程式可以註冊執行階段模組：

```csharp
using Autofac;
using B2B.CryptoLib;

var builder = new ContainerBuilder();
builder.RegisterModule(new CryptoSuiteModule(@"D:\B2B\Keys", "B2B_20260901"));
using var container = builder.Build();
```

`CryptoSuiteModule` 會以單例註冊執行階段服務。它不是必要元件，也支援
直接建構 `CryptoClient`。獨立的 `KeyGenerationModule` 屬於離線金鑰產生工具，
不應註冊到 Web／執行階段容器。

## Key Generation（金鑰產生）

金鑰產生屬於離線職責。工具會透過舊版靜態 `CryptoConfig` API 讀取複製的
`appsettings.json`，並支援 `AES`、`RSA`、`ECC` 與 `KEYSET` 命令：

```powershell
dotnet run --project .\B2B.CryptoLib.KeyGenTool -- KEYSET sample-20260902
```

輸出包含秘密性的 AES 材料與私鑰 PEM。請將其保存在受保護的離線位置，
並且只透過核准的金鑰發布流程傳送。

## Key Publication（金鑰發布）

`KEYSET` 輸出會暫存於設定金鑰根目錄的 `update` 資料夾。只有明確呼叫
`Crypto.UpdateKeySetsAsync()` 或 `KeyManagerService.StartAsync()` 才會處理它。
完整金鑰組會依公開金鑰、私鑰、AES 材料的順序發布，AES 材料最後寫入；
暫存檔與原子取代可避免讀取者看到單一檔案的半成品。更新成功後會
消費暫存檔，並清除執行更新之用戶端的金鑰快取。不要在每個請求上執行
這個會修改檔案系統的操作。

輪替、歷史金鑰、回復、備份與權限要求請參閱
[金鑰管理](docs/KEY-MANAGEMENT.md)。

## Security / Query Semantics（安全性／查詢語意）

- 新的 GCM 密文使用隨機 nonce（隨機數），因此相同明文可能產生不同密文。不要把密文
  相等性當成確定性資料庫查詢鍵。
- 外層值格式為 `Base64(payload).unifiedName`；新的載荷也會將統一名稱作為
  GCM AAD 進行驗證綁定。
- `IsValidEncryptedFormat` 只檢查外層形狀，不能證明訊息驗證、授權、
  金鑰存在或可解密。
- 金鑰根目錄應位於原始碼樹、Web 根目錄與任何不受信任使用者可寫入的目錄
  之外。絕對不要提交 `.aes`、`.pub`、`.priv`、`.der`、`.public.pem`、
  `.private.pem` 或含金鑰位元組的產生 JSON。

## Compatibility（相容性）

套件識別名稱的變更不會改變公開密碼 API、密文封裝格式、GCM v2 載荷、
金鑰檔案配置、RSA 模式、ECC PEM／簽章流程或舊版解密行為。沒有 GCM
標記的載荷仍會使用舊版 AES-CBC／PKCS#7 讀取路徑。

變更密碼程式碼、金鑰序列化或相依套件配接器前，請先閱讀
[密碼相容性契約](docs/CRYPTO-COMPATIBILITY.md)。

## Offline Packaging（離線封裝）

使用儲存庫的 .NET 10 Microsoft Testing Platform 設定，對同一候選版本執行
建置、測試與封裝：

```powershell
dotnet restore B2B.CryptoLib.sln
dotnet build B2B.CryptoLib.sln -c Release --no-restore
dotnet test B2B.CryptoLib.sln -c Release --no-build
dotnet pack .\B2B.CryptoLib\B2B.CryptoLib.csproj -c Release --no-build -o <offline-feed-directory>
```

驗證傳送內容時，請使用新的套件快取並只指定預期的離線來源。
完整程序與 SHA-256 完整性檢查請參閱
[離線封裝](docs/OFFLINE-PACKAGING.md)。

## Documentation（文件）

- [架構](docs/ARCHITECTURE.md)
- [金鑰管理](docs/KEY-MANAGEMENT.md)
- [密碼相容性契約](docs/CRYPTO-COMPATIBILITY.md)
- [離線封裝](docs/OFFLINE-PACKAGING.md)
- [變更記錄](CHANGELOG.md)

## Dependencies（相依套件）

執行階段的直接相依套件：

- `Autofac` 9.3.2
- `BouncyCastle.Cryptography` 2.7.0，取代 `Portable.BouncyCastle` 1.9.0
- `Newtonsoft.Json` 13.0.4

測試專案使用 xUnit v3 4.0.0、`Microsoft.NET.Test.Sdk` 18.9.0 與
`xunit.runner.visualstudio` 4.0.0。正式環境與 KeyGeneration 專案會輸出
供 IntelliSense 及離線開發使用的 XML 文件檔。

## Versioning（版本）

2.0.1 是完成相依套件現代化的套件候選版本，仍只支援 .NET 10。
相依套件遷移與下游注意事項請參閱 [CHANGELOG.md](CHANGELOG.md)，尤其是
[從 2.0.0 升級至 2.0.1](CHANGELOG.md#從-200-升級至-201)。

下游 `B2B_API` 後續需要另外將 `B2B.CryptoLib` 從 2.0.0 更新至 2.0.1，並將
Autofac 從 9.1.0 更新至 9.3.2。這次是僅限 CryptoLib 的變更，不會修改該
儲存庫。
