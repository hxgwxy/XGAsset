using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using XGAsset.Editor.Settings;
using XGAsset.Editor.Settings;

namespace XGAsset.Editor.Other
{
    public interface IAddressRule
    {
        public void CalcAddress(AssetAddressEntry entry);
    }

    /// <summary>
    /// 通用的地址规则
    /// </summary>
    public class NoneAddressRule : IAddressRule
    {
        public void CalcAddress(AssetAddressEntry entry)
        {
            // entry.Address = entry.AssetPath;
        }
    }

    /// <summary>
    /// 简化地址规则
    /// </summary>
    public class SimplifyAddressRule : IAddressRule
    {
        public void CalcAddress(AssetAddressEntry entry)
        {
            AssetAddressDefaultSettings.ChangeAssetAddress(entry.Address, Path.GetFileNameWithoutExtension(entry.AssetPath));
        }
    }

    /// <summary>
    /// 弹窗规则
    /// </summary>
    public class PanelAddressRule : IAddressRule
    {
        public void CalcAddress(AssetAddressEntry entry)
        {
            AssetAddressDefaultSettings.ChangeAssetAddress(entry.Address, Path.GetFileNameWithoutExtension(entry.AssetPath));
        }
    }

    public class OriginAddressRule : IAddressRule
    {
        public void CalcAddress(AssetAddressEntry entry)
        {
            AssetAddressDefaultSettings.ChangeAssetAddress(entry.Address, entry.AssetPath);
        }
    }

    /// <summary>
    /// 地图地址
    /// </summary>
    public class MapAddressRule : IAddressRule
    {
        public void CalcAddress(AssetAddressEntry entry)
        {
            var patternList = new List<string>()
            {
                @"(Level_\d+\/\w+.png)",
                @"(Level_\d+\/\d+\/\d+.png)",
            };
            foreach (var pattern in patternList)
            {
                var match = Regex.Match(entry.AssetPath, pattern);
                if (match.Success)
                {
                    AssetAddressDefaultSettings.ChangeAssetAddress(entry.Address, match.Groups[1].Value);
                }
            }
        }
    }
}