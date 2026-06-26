Add-Type -Path "d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\bin\Debug\net10.0-windows\Microsoft.Data.Sqlite.dll"
$conn = [Microsoft.Data.Sqlite.SqliteConnection]::new("Data Source=d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT DISTINCT MaterialType FROM FactoryMaterials WHERE MaterialType IS NOT NULL AND MaterialType != '' ORDER BY MaterialType"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host $reader[0]
}
$conn.Close()
