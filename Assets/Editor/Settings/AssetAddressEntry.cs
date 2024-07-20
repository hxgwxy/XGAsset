using System;
using System.Collections.Generic;

namespace XGFramework.XGAsset.Editor.Settings
{
    [Serializable]
    public class AssetAddressEntry
    {
        public string GroupName;
        public string AssetPath = string.Empty;
        public string Address = string.Empty;
        public string Guid = string.Empty;
        public string MainType;
        public string AddressRuleName;
        public List<string> Labels = new List<string>();
        public bool Active = true;
        
        public void Addlabel(string label)
        {
            Labels ??= new List<string>();
            if (!Labels.Contains(label))
            {
                Labels.Add(label);
                Labels.Sort();
            }
        }

        public void RemoveLabel(string label)
        {
            Labels?.Remove(label);
        }

        public bool HasLabel(string label)
        {
            return Labels?.Contains(label) ?? false;
        }
    }
}