# Cryptographic Compatibility Contract（密碼相容性契約）

修改 CryptoLib 的密碼程式碼、金鑰處理、序列化、相依套件配接器或產生檔名前，
請先閱讀本文件。只有在下列契約維持不變且回歸測試固定資料持續通過時，套件更新
才可視為安全。

## Ciphertext envelope（密文 Envelope）

高階文字加密使用以下表示方式：

```text
Base64(payload).unifiedName
```

外層尾綴同時是金鑰組查找名稱，也是新 GCM 載荷的驗證執行脈絡。它本身不是
簽章。`IsValidEncryptedFormat` 可以檢查外層形狀，但該方法不會驗證或解密載荷。

## GCM v2 payload（GCM v2 Payload）

新的高階寫入使用以下位元組配置：

| 位移 | 大小 | 欄位 | 契約 |
| ---: | ---: | --- | --- |
| 0 | 7 | Magic（標記） | ASCII `B2BCGCM` |
| 7 | 1 | Version（版本） | byte 值 `2` |
| 8 | 12 | Nonce（隨機數） | 每次加密都必須是新的隨機 nonce |
| 20 | 剩餘長度 | Ciphertext and tag（密文與標籤） | AES-GCM 密文後接 16 位元組標籤 |

載荷會進行 Base64 編碼後放入外層字串。此封裝格式不含確定性 nonce
或由明文推導的值，因此相同明文與統一名稱不保證產生相同密文。

標籤是最後密文位元組的一部分。長度不足、版本不支援或標籤驗證失敗的載荷
都會被拒絕。Magic 與 version 只是格式辨識值，不能取代訊息驗證。

## AAD（附加驗證資料）

`unifiedName` 尾綴會以 UTF-8 編碼，並作為 GCM 附加驗證資料
（AAD）傳入。它不會被加密，但會與密文綁定。若把尾綴改成另一個有效金鑰名稱，
讀取器會選取不同金鑰與 AAD；真正的 GCM 載荷會驗證失敗，不會靜默地使用
新名稱解密。

這個設計刻意不是確定性資料庫查找機制。不要把密文相等性當作
索引，也不要假設重複值一定有重複密文。若應用程式需要明確的查找或路由
鍵，請另外儲存統一名稱。

## Legacy CBC fallback（Legacy CBC 備援）

沒有 `B2BCGCM` 標記的載荷會使用舊版 AES-CBC 與 PKCS#7 填充路徑。
這個分支是為了相容 GCM v2 之前寫入的歷史密文；只要歷史資料仍須讀取，就必須
保留它。

備援路徑不是產生新 CBC 資料的理由。新寫入仍使用 GCM v2。沒有規劃格式遷移與
主要版本決策前，不要移除、重新排序或重新解讀沒有標記的回退路徑。

## RSA OAEP（RSA OAEP）

目前 RSA 資料路徑使用 OAEP，搭配目前的 `.aes`／`.pub`／`.priv` 契約。
`CryptoService` 使用 OAEP 進行目前 RSA 包裝，目前的金鑰組產生流程也用它
包裝新產生的 AES 材料。

變更 OAEP 參數、PEM 解析、金鑰選擇或包裝材料編碼，可能使既有金鑰組無法
讀取。任何變更前都要先測試目前的金鑰組測試固定資料與往返測試。

## RSA PKCS#1 legacy（Legacy RSA PKCS#1）

歷史 `.der` 金鑰組路徑使用 RSA PKCS#1 v1.5 包裝 AES 材料，並搭配相應的
舊版句點分隔材料格式。此路徑由 `LegacyKeySetCrypto` 實作，刻意與目前
的 OAEP 路徑分離；兩種包裝模式不可互換。

讀取器也保留一個範圍狹窄的過渡回退路徑，支援某個中間版本以 OAEP 與冒號分隔
材料產生的 `.der` 檔案。這項相容行為屬於既有讀取器契約，不得任意移除。

## ECC（ECC）

ECC 是簽章／驗章路徑，不是高階文字加密路徑。離線產生器支援 NIST P-256、
NIST P-384、NIST P-521 與 secp256k1。金鑰以既有模型格式序列化為 PEM，現有
簽章路徑使用 SHA-256 搭配 ECDSA。

曲線識別值、PEM 標籤、私鑰／公鑰角色與簽章演算法選擇都是相容性輸入。變更
曲線或 PEM 格式前，必須使用既有測試固定資料與使用端驗證。

## Key layouts（金鑰配置）

執行階段辨識完整組合，而不是單一檔案：

| 配置 | 必要檔案 | 意義 |
| --- | --- | --- |
| v2 | `<name>.aes`、`<name>.pub`、`<name>.priv` | RSA 包裝的 AES 材料，以及 RSA 公開／私密 PEM。 |
| legacy | `<name>.der`、`<name>.public.pem`、`<name>.private.pem` | 舊版 RSA 包裝材料，以及相應的 PEM 金鑰對。 |

完整組合可以位於 `current`、`history` 或 `update`。`update` 在明確發布前只是
暫存區。AES 檔案是發布時的辨識標記，因此最後寫入。不要只靠副檔名
丟棄讀取器的過渡相容路徑。

## UnifiedName（統一名稱）

`unifiedName` 只能包含英文字母、數字、`_` 與 `-`，不可含句點。它同時用作
檔案安全的金鑰組識別值、外層密文尾綴與 UTF-8 GCM AAD。它不是秘密，也不應包含
路徑語法。

解密會從密文尾綴選取金鑰組。`ActiveUnifiedName` 只選擇新加密的預設金鑰，
不會取代尾綴，也不控制歷史解密。

## Rotation compatibility（輪替相容性）

輪替時應使用新的統一名稱、把舊的完整組合保留在 `history`、將新的三個檔案
全部暫存到 `update`，最後明確呼叫一次更新。管理器會優先搜尋 `current` 再
搜尋 `history`，並在成功發布後清除自己的快取。它不會自動把舊 `current` 檔案
搬到 `history`，也不會協調共用根目錄的其他用戶端或程序。

只要保留尾綴相符的歷史金鑰組與私密材料，GCM v2 與舊版密文就仍可讀取。
絕對不要用刪除舊資料唯一金鑰的方式測試輪替。

## Regression fixtures（回歸 Fixtures）

相容性測試套件涵蓋：

- AES-CBC 往返測試與固定的已知答案向量；
- GCM v2 往返測試、不支援的版本與遭竄改的載荷；
- 舊版 `.der`／PEM 金鑰組載入與舊版 AES-CBC 解密；
- current 與 history 金鑰查找；
- 輪替後的新舊金鑰版本；
- 新舊金鑰組的交叉解密；
- 統一名稱的 AAD 綁定；
- 不完整的 update 組合與暫存檔消費；
- 替換既有 unified name 時的快取失效；
- RSA 與 ECC 簽章驗證，以及變更資料／簽章後的失敗。

升級密碼相依套件時，請保留這些測試與外部保存的密文測試固定資料。建置通過本身
不足以證明格式相容。

## Changes requiring a major version（需要 Major Version 的變更）

下列任一項改變都屬於密碼格式或金鑰契約變更，不是一般相依套件更新：

```text
ciphertext envelope
magic
version
nonce/tag layout
AAD
padding
PEM layout
key wrapping
舊版解密
```

這類變更需要書面的遷移設計、新舊測試固定資料覆蓋範圍、明確的資料／金鑰遷移或
雙讀取策略、下游審查與主要版本決策。不要把它藏在套件更新中，也不要
只因替代密碼函式庫提供不同 API 就修改它。
