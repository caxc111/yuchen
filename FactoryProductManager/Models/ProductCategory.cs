using System.Collections.Generic;

namespace FactoryProductManager.Models
{
    /// <summary>
    /// 产品类别（支持多级分类）
    /// </summary>
    public class ProductCategory
    {
        public string Name { get; set; } = string.Empty;
        public List<ProductCategory> Children { get; set; } = new List<ProductCategory>();
    }

    /// <summary>
    /// 产品类别数据（第一级与工厂类型统一）
    /// </summary>
    public static class ProductCategoryData
    {
        /// <summary>
        /// 获取工厂类型列表（用于工厂管理）
        /// </summary>
        public static List<string> GetFactoryTypes()
        {
            return new List<string>
            {
                "柜体木饰面",
                "木地板",
                "地毯",
                "瓷砖",
                "石材",
                "厨卫陶瓷",
                "厨卫五金",
                "户内门",
                "灯具开关",
                "电器"
            };
        }

        /// <summary>
        /// 获取产品分类（第一级与工厂类型一致）
        /// </summary>
        public static List<ProductCategory> GetCategories()
        {
            return new List<ProductCategory>
            {
                new ProductCategory
                {
                    Name = "柜体木饰面",
                    Children = new List<ProductCategory>
                    {
                        new ProductCategory { Name = "实木皮饰面板" },
                        new ProductCategory { Name = "科技木皮饰面板" },
                        new ProductCategory { Name = "三聚氰胺饰面板" },
                        new ProductCategory { Name = "PET膜饰面板" },
                        new ProductCategory { Name = "混油饰面板" },
                        new ProductCategory { Name = "其他饰面" },
                        new ProductCategory
                        {
                            Name = "柜体、家具五金",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "铰链" },
                                new ProductCategory { Name = "轨道" },
                                new ProductCategory { Name = "拉手" },
                                new ProductCategory { Name = "成品抽屉" },
                                new ProductCategory { Name = "铝合金柜门" }
                            }
                        }
                    }
                },
                new ProductCategory
                {
                    Name = "木地板",
                    Children = new List<ProductCategory>
                    {
                        new ProductCategory { Name = "实木地板" },
                        new ProductCategory { Name = "实木复合地板" },
                        new ProductCategory { Name = "强化复合地板" },
                        new ProductCategory { Name = "SPC(石塑)" },
                        new ProductCategory { Name = "LVT" },
                        new ProductCategory { Name = "WPC（木塑）" },
                        new ProductCategory { Name = "塑胶地板及其他" }
                    }
                },
                new ProductCategory
                {
                    Name = "地毯",
                    Children = new List<ProductCategory>
                    {
                        new ProductCategory { Name = "丙纶满铺毯" },
                        new ProductCategory { Name = "晴纶满铺毯" },
                        new ProductCategory { Name = "涤纶满铺毯" },
                        new ProductCategory { Name = "羊毛/羊毛混纺" },
                        new ProductCategory { Name = "植物纤维" }
                    }
                },
                new ProductCategory
                {
                    Name = "瓷砖",
                    Children = new List<ProductCategory>
                    {
                        new ProductCategory { Name = "亮面砖" },
                        new ProductCategory { Name = "哑光砖" },
                        new ProductCategory { Name = "肌理/手工/仿古砖" },
                        new ProductCategory { Name = "马赛克/小砖" },
                        new ProductCategory { Name = "岩板/大规格瓷砖" }
                    }
                },
                new ProductCategory
                {
                    Name = "石材",
                    Children = new List<ProductCategory>
                    {
                        new ProductCategory { Name = "大理石" },
                        new ProductCategory { Name = "花岗岩" },
                        new ProductCategory { Name = "砂岩" },
                        new ProductCategory { Name = "板岩" },
                        new ProductCategory { Name = "石灰石" },
                        new ProductCategory
                        {
                            Name = "人造石",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "树脂基石英石" },
                                new ProductCategory { Name = "无机基石英石" },
                                new ProductCategory { Name = "人造大理石（岗石）" },
                                new ProductCategory { Name = "微晶石" }
                            }
                        }
                    }
                },
                new ProductCategory
                {
                    Name = "厨卫陶瓷",
                    Children = new List<ProductCategory>
                    {
                        new ProductCategory
                        {
                            Name = "座便器",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "连体马桶" },
                                new ProductCategory { Name = "分体马桶" },
                                new ProductCategory { Name = "壁挂马桶" },
                                new ProductCategory { Name = "智能马桶" },
                                new ProductCategory { Name = "智能马桶盖" }
                            }
                        },
                        new ProductCategory
                        {
                            Name = "台盆",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "台上盆" },
                                new ProductCategory { Name = "台中盆（半嵌盆）" },
                                new ProductCategory { Name = "台下盆" },
                                new ProductCategory { Name = "一体盆" },
                                new ProductCategory { Name = "挂墙盆/立柱盆" }
                            }
                        },
                        new ProductCategory
                        {
                            Name = "水槽",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "单槽洗菜盆" },
                                new ProductCategory { Name = "双槽洗菜盆" },
                                new ProductCategory { Name = "异形洗菜盆" },
                                new ProductCategory { Name = "石英石洗菜盆" }
                            }
                        },
                        new ProductCategory
                        {
                            Name = "拖把池",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "落地式拖把池" },
                                new ProductCategory { Name = "壁挂式拖把池" }
                            }
                        },
                        new ProductCategory
                        {
                            Name = "浴缸",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "普通浴缸" },
                                new ProductCategory { Name = "按摩浴缸" }
                            }
                        }
                    }
                },
                new ProductCategory
                {
                    Name = "厨卫五金",
                    Children = new List<ProductCategory>
                    {
                        new ProductCategory
                        {
                            Name = "龙头",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "面盆龙头" },
                                new ProductCategory { Name = "厨房龙头" },
                                new ProductCategory { Name = "淋浴龙头" },
                                new ProductCategory { Name = "其他龙头" }
                            }
                        },
                        new ProductCategory
                        {
                            Name = "淋浴屏风",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "浴屏玻璃" },
                                new ProductCategory { Name = "浴屏五金" },
                                new ProductCategory { Name = "屏风挡水" },
                                new ProductCategory { Name = "淋浴房底座" }
                            }
                        },
                        new ProductCategory
                        {
                            Name = "收纳架",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "毛巾架" },
                                new ProductCategory { Name = "厕纸架" },
                                new ProductCategory { Name = "浴巾架" },
                                new ProductCategory { Name = "置物架" }
                            }
                        },
                        new ProductCategory { Name = "地漏/角阀/软管等" }
                    }
                },
                new ProductCategory
                {
                    Name = "户内门",
                    Children = new List<ProductCategory>
                    {
                        new ProductCategory { Name = "木门" },
                        new ProductCategory { Name = "铝合金门窗" },
                        new ProductCategory
                        {
                            Name = "门窗五金配件",
                            Children = new List<ProductCategory>
                            {
                                new ProductCategory { Name = "门锁" },
                                new ProductCategory { Name = "合页" },
                                new ProductCategory { Name = "拉手" },
                                new ProductCategory { Name = "门碰" }
                            }
                        }
                    }
                },
                new ProductCategory
                {
                    Name = "灯具开关",
                    Children = new List<ProductCategory>
                    {
                        new ProductCategory { Name = "筒灯/射灯" },
                        new ProductCategory { Name = "灯带" },
                        new ProductCategory { Name = "吊灯" },
                        new ProductCategory { Name = "壁灯" },
                        new ProductCategory { Name = "开关插座" },
                        new ProductCategory { Name = "浴霸/排气扇" }
                    }
                },
                new ProductCategory
                {
                    Name = "电器",
                    Children = new List<ProductCategory>
                    {
                        new ProductCategory { Name = "油烟机" },
                        new ProductCategory { Name = "燃气灶" },
                        new ProductCategory { Name = "电磁灶" },
                        new ProductCategory { Name = "洗碗机" },
                        new ProductCategory { Name = "微波炉" },
                        new ProductCategory { Name = "蒸烤箱" },
                        new ProductCategory { Name = "热水器" },
                        new ProductCategory { Name = "冰箱" },
                        new ProductCategory { Name = "中央空调/新风/地暖设备" }
                    }
                }
            };
        }
    }
}
