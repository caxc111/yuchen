using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FactoryProductManager.Models;

namespace FactoryProductManager.Services
{
    public static class MaterialCodeGenerator
    {
        private static readonly Dictionary<string, string> Level1Codes = new(StringComparer.Ordinal)
        {
            ["柜体木饰面"] = "SM",
            ["木地板"] = "MD",
            ["地毯"] = "DT",
            ["瓷砖"] = "CZ",
            ["石材"] = "SC",
            ["厨卫陶瓷"] = "CW",
            ["厨卫五金"] = "WJ",
            ["户内门"] = "HM",
            ["灯具开关"] = "DJ",
            ["电器"] = "DQ"
        };

        private static readonly Dictionary<string, string> Level2Codes = new(StringComparer.Ordinal)
        {
            ["实木皮饰面板"] = "SP",
            ["科技木皮饰面板"] = "KJ",
            ["三聚氰胺饰面板"] = "SJ",
            ["PET膜饰面板"] = "PT",
            ["混油饰面板"] = "HY",
            ["其他饰面"] = "QT",
            ["实木地板"] = "SM",
            ["实木复合地板"] = "SF",
            ["强化复合地板"] = "QH",
            ["SPC(石塑)"] = "SB",
            ["LVT"] = "LT",
            ["WPC（木塑）"] = "WP",
            ["塑胶地板及其他"] = "BQ",
            ["丙纶满铺毯"] = "BL",
            ["晴纶满铺毯"] = "QC",
            ["涤纶满铺毯"] = "JD",
            ["羊毛/羊毛混纺"] = "YM",
            ["植物纤维"] = "ZQ",
            ["亮面砖"] = "LM",
            ["哑光砖"] = "YG",
            ["肌理/手工/仿古砖"] = "JL",
            ["马赛克/小砖"] = "MK",
            ["岩板/大规格瓷砖"] = "YB",
            ["大理石"] = "DL",
            ["花岗岩"] = "HG",
            ["砂岩"] = "FA",
            ["板岩"] = "BY",
            ["石灰石"] = "SH",
            ["筒灯/射灯/灯带"] = "TD",
            ["吊灯"] = "DD",
            ["壁灯"] = "BD",
            ["开关插座面板"] = "KZ",
            ["浴霸/排气扇"] = "YF",
            ["坐厕类"] = "ZC",
            ["台盆类"] = "TP",
            ["水槽类"] = "SK",
            ["拖把池"] = "DM",
            ["浴缸"] = "YK",
            ["面盆龙头"] = "ML",
            ["厨房龙头"] = "KL",
            ["淋浴龙头"] = "LL",
            ["其他龙头"] = "QL",
            ["毛巾架"] = "MJ",
            ["置物架"] = "ZW",
            ["地漏/角阀/软管等"] = "DF",
            ["木门"] = "MM",
            ["铝合金门窗"] = "LC",
            ["油烟机"] = "YY",
            ["燃气灶"] = "RZ",
            ["电磁灶"] = "DC",
            ["洗碗机"] = "XW",
            ["微波炉"] = "WB",
            ["蒸烤箱"] = "ZK",
            ["热水器"] = "RS",
            ["冰箱"] = "BX",
            ["中央空调/新风/地暖设备"] = "ZN"
        };

        private static readonly Dictionary<string, string> Level3Codes = new(StringComparer.Ordinal)
        {
            // 柜体木饰面 > 柜体、家具五金
            ["铰链"] = "JL",
            ["轨道"] = "GD",
            ["拉手"] = "LS",
            ["成品抽屉"] = "CT",
            ["铝合金柜门"] = "LM",
            // 石材 > 人造石
            ["树脂基石英石"] = "SZ",
            ["无机基石英石"] = "SY",
            ["人造大理石（岗石）"] = "GS",
            ["微晶石"] = "WJ",
            // 厨卫陶瓷 > 坐厕类
            ["连体马桶"] = "LT",
            ["分体马桶"] = "FT",
            ["壁挂马桶"] = "BG",
            ["智能马桶"] = "ZN",
            ["智能马桶盖"] = "ZG",
            // 厨卫陶瓷 > 台盆类
            ["台上盆"] = "TS",
            ["台中盆（半嵌盆）"] = "TZ",
            ["台下盆"] = "TX",
            ["一体盆"] = "YT",
            ["挂墙盆/立柱盆"] = "GQ",
            // 厨卫陶瓷 > 水槽类
            ["单槽洗菜盆"] = "DC",
            ["双槽洗菜盆"] = "SC",
            ["异形洗菜盆"] = "YX",
            ["石英石洗菜盆"] = "SQ",
            // 厨卫陶瓷 > 拖把池
            ["落地式拖把池"] = "LD",
            ["壁挂式拖把池"] = "BM",
            // 厨卫陶瓷 > 浴缸
            ["普通浴缸"] = "PT",
            ["按摩浴缸"] = "AM",
            // 厨卫五金 > 淋浴屏风
            ["浴屏玻璃"] = "YB",
            ["浴屏五金"] = "YW",
            ["屏风挡水"] = "PD",
            ["淋浴房底座"] = "LY",
            // 户内门 > 门窗五金配件
            ["门锁"] = "MS",
            ["合页"] = "HY",
            ["门碰"] = "MP"
        };

        public static bool TryGenerate(
            DbService dbService,
            FactoryMaterial material,
            Factory factory,
            ProductCategory? level1,
            ProductCategory? level2,
            ProductCategory? level3,
            out string code,
            out string errorMessage)
        {
            code = string.Empty;
            errorMessage = string.Empty;

            if (dbService == null)
            {
                errorMessage = "编码服务未初始化。";
                return false;
            }

            if (material == null)
            {
                errorMessage = "当前物料数据无效。";
                return false;
            }

            if (factory == null || string.IsNullOrWhiteSpace(factory.FactoryCode))
            {
                errorMessage = "请先选择有效的工厂编码。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(material.FactoryMaterialCode))
            {
                errorMessage = "请先填写工厂物料编码。";
                return false;
            }

            if (level1 == null)
            {
                errorMessage = "请先选择一级分类。";
                return false;
            }

            if (!Level1Codes.TryGetValue(level1.Name, out string? level1Code) || string.IsNullOrWhiteSpace(level1Code))
            {
                errorMessage = $"未找到一级分类“{level1.Name}”对应的编码。";
                return false;
            }

            string tailCode = string.Empty;
            if (level3 != null)
            {
                if (!Level3Codes.TryGetValue(level3.Name, out string? resolvedLevel3Code) || string.IsNullOrWhiteSpace(resolvedLevel3Code))
                {
                    errorMessage = $"未找到三级分类“{level3.Name}”对应的编码。";
                    return false;
                }

                tailCode = resolvedLevel3Code;
            }
            else
            {
                if (level2 == null)
                {
                    errorMessage = "请先选择二级分类。";
                    return false;
                }

                if (!Level2Codes.TryGetValue(level2.Name, out string? resolvedLevel2Code) || string.IsNullOrWhiteSpace(resolvedLevel2Code))
                {
                    errorMessage = $"未找到二级分类“{level2.Name}”对应的编码。";
                    return false;
                }

                tailCode = resolvedLevel2Code;
            }

            string? existingCode = dbService.GetMyMaterialCodeByFactoryMaterialCode(material.FactoryMaterialCode, material.Id > 0 ? material.Id : null);
            if (!string.IsNullOrWhiteSpace(existingCode))
            {
                code = existingCode;
                return true;
            }

            string prefix = string.Join("-", new[] { factory.FactoryCode.Trim(), level1Code, tailCode }.Where(part => !string.IsNullOrWhiteSpace(part)));
            int nextSequence = dbService.GetNextMyMaterialCodeSequence(prefix, material.Id > 0 ? material.Id : null);
            code = $"{prefix}-{nextSequence.ToString("000", CultureInfo.InvariantCulture)}";
            return true;
        }
    }
}
