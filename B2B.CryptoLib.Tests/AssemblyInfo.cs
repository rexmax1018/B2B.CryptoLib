using Xunit.Sdk;
using Xunit.v3;

// 測試會變更程序層級的密碼設定並共用金鑰組測試固定資料；
// 這是 xUnit v3 對應舊版 v2 DisableTestParallelization 設定的寫法。
[assembly: Parallelization(Mode = ParallelMode.None)]
