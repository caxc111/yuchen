import xlrd

# 读取工厂产品信息
try:
    wb = xlrd.open_workbook('工厂产品信息.xls')
    sheet = wb.sheet_by_index(0)
    print("===== 工厂产品信息.xls =====")
    print("表头:", sheet.row_values(0))
    if sheet.nrows > 1:
        print("示例数据:", sheet.row_values(1))
except Exception as e:
    print(f"读取工厂产品信息失败: {e}")

print()

# 读取工厂信息
try:
    wb = xlrd.open_workbook('工厂信息.xls')
    sheet = wb.sheet_by_index(0)
    print("===== 工厂信息.xls =====")
    print("表头:", sheet.row_values(0))
    if sheet.nrows > 1:
        print("示例数据:", sheet.row_values(1))
except Exception as e:
    print(f"读取工厂信息失败: {e}")