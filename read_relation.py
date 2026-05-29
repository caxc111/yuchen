import xlrd
import sys
import io

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# 读取工厂信息
factories = {}
try:
    wb = xlrd.open_workbook('工厂信息.xls')
    sheet = wb.sheet_by_index(0)
    headers = sheet.row_values(0)
    factory_name_idx = headers.index('工厂名称')
    
    for i in range(1, sheet.nrows):
        row = sheet.row_values(i)
        factory_name = row[factory_name_idx]
        factories[factory_name] = row
    print(f"读取到 {len(factories)} 家工厂")
    print("工厂列表:", list(factories.keys()))
except Exception as e:
    print(f"读取工厂信息失败: {e}")

# 读取工厂产品信息并分析关联
products = []
try:
    wb = xlrd.open_workbook('工厂产品信息.xls')
    sheet = wb.sheet_by_index(0)
    headers = sheet.row_values(0)
    product_name_idx = headers.index('产品名称')
    factory_idx = headers.index('所属工厂')
    
    print("\n===== 产品与工厂关联关系 =====")
    for i in range(1, sheet.nrows):
        row = sheet.row_values(i)
        product_name = row[product_name_idx]
        factory_name = row[factory_idx]
        
        if factory_name in factories:
            status = "已关联"
        else:
            status = "未找到匹配工厂"
        
        print(f"{i}. 产品: {product_name}")
        print(f"   所属工厂: {factory_name}")
        print(f"   状态: {status}")
        print()
        
        products.append({
            'product_name': product_name,
            'factory_name': factory_name,
            'matched': factory_name in factories
        })
    
    matched = sum(1 for p in products if p['matched'])
    print(f"统计：共 {len(products)} 个产品，{matched} 个已关联到工厂，{len(products)-matched} 个未关联")
    
except Exception as e:
    print(f"读取工厂产品信息失败: {e}")
    import traceback
    traceback.print_exc()