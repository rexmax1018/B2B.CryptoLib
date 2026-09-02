# Offline packaging（離線封裝）

本指南說明如何在移往沒有網際網路存取的環境前，建立並驗證 CryptoLib 套件
候選版本。命令使用佔位符，因此不會提交機器專用路徑或秘密。

## Build candidate（建立候選版本）

請在儲存庫根目錄執行，並使用儲存庫 `global.json` 指定的 .NET 10 SDK：

```powershell
dotnet restore B2B.CryptoLib.sln
dotnet build B2B.CryptoLib.sln -c Release --no-restore
dotnet test B2B.CryptoLib.sln -c Release --no-build
```

`global.json` 選用 Microsoft Testing Platform 執行器。測試專案是供 xUnit v3
使用的可執行檔，因此上面的方案層級 `dotnet test` 是本儲存庫支援的完整測試
命令；不要改用只支援 v2 的執行器命令。

預期結果是無警告的 Release 建置與完整測試套件全部通過。調查相依套件
變更時，也請執行下列指定的密碼測試套件：

```powershell
dotnet test --project .\B2B.CryptoLib.Tests\B2B.CryptoLib.Tests.csproj `
  -c Release `
  --no-build `
  --filter FullyQualifiedName~CryptoServiceBehaviorTests

dotnet test --project .\B2B.CryptoLib.Tests\B2B.CryptoLib.Tests.csproj `
  -c Release `
  --no-build `
  --filter FullyQualifiedName~CryptoSuiteIntegrationTests
```

## Pack（封裝）

建置成功後再封裝執行階段專案。請把佔位符換成暫存目錄，不要使用受版本
控制的目錄：

```powershell
dotnet pack .\B2B.CryptoLib\B2B.CryptoLib.csproj `
  -c Release `
  --no-build `
  -o <offline-feed-directory>
```

產出的套件為 `B2B.CryptoLib.2.0.1.nupkg`。正式環境專案已啟用 XML
文件輸出，因此請檢查壓縮檔並至少要求下列項目：

```text
lib/net10.0/B2B.CryptoLib.dll
lib/net10.0/B2B.CryptoLib.xml
README.md
B2B.CryptoLib.nuspec
```

KeyGeneration 組件也會在自己的建置輸出旁產生 XML 文件，但不會被放入
執行階段套件。

## Integrity（完整性）

請計算實際要傳送的套件檔案雜湊值：

```powershell
$offlineFeed = '<offline-feed-directory>'
Get-FileHash (Join-Path $offlineFeed 'B2B.CryptoLib.2.0.1.nupkg') -Algorithm SHA256
```

請將結果與發行候選版本中繼資料一起記錄，並在傳送後再次驗證。若在文件
或原始碼變更後重新建置套件，它就是新的候選版本，必須使用新的雜湊值。

## Offline feed（離線來源）

將套件與相依套件圖所需的每個套件複製到離線來源目錄。在離線主機用
符合該主機的路徑註冊來源：

```powershell
dotnet nuget add source '<offline-feed-directory>' --name CryptoLibOffline
```

還原時請明確使用預期的離線來源。不要提交開發者專用路徑、NuGet.Config、
憑證或快取目錄。

## Clean restore verification（乾淨還原驗證）

請使用新的套件快取並只允許指定來源，避免線上全域快取掩蓋缺少的套件。
以下是 PowerShell 範例；請選擇離線主機允許的暫存位置：

```powershell
$offlineFeed = (Resolve-Path '<offline-feed-directory>').Path
$offlinePackageCache = Join-Path ([System.IO.Path]::GetTempPath()) 'B2B.CryptoLib-offline-packages'
New-Item -ItemType Directory -Force -Path $offlinePackageCache | Out-Null
$env:NUGET_PACKAGES = $offlinePackageCache

dotnet restore .\B2B.CryptoLib.sln `
  --source $offlineFeed `
  --force-evaluate
```

若方案的測試專案也屬於離線驗證範圍，來源必須同時包含 xUnit v3、
Microsoft Testing Platform／Test SDK 與配接器套件。請在停用網路的狀態，或在
真正的離線主機上執行還原，證明指定來源足以解析完整套件圖。驗證完成後，依
主機清理政策移除暫存快取。

## Required transitive dependencies（必要的 Transitive Dependencies）

執行階段套件的相依套件圖必須能從離線來源取得，包括：

```text
Autofac 9.3.2
BouncyCastle.Cryptography 2.7.0
Newtonsoft.Json 13.0.4
```

套件 nuspec 是相依套件宣告的權威來源。不要只複製執行階段 DLL 就假設套件
自給自足；還原必須能從指定來源解析宣告的套件圖。

## Package and source checks（套件與來源檢查）

傳送前請將套件中繼資料與已提交的專案檔比對，確認執行階段套件圖
不存在 `Portable.BouncyCastle`，並確認套件與儲存庫都沒有金鑰檔案或產生的
秘密材料。最後執行：

```powershell
git diff --check
git status --short
```

工作樹應只包含有意的原始碼／文件變更；建置輸出與套件候選檔案應維持在
忽略規則內或儲存庫之外。
