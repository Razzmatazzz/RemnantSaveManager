using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RemnantSaveManager
{
    public class RemnantItem : IEquatable<Object>, IComparable
    {
        public enum RemnantItemMode
        {
            Normal,
            Hardcore,
            Survival
        }

        private string itemKey;
        private string itemType;
        private string itemName;
        private string ItemKey { 
            get { return itemKey; } 
            set {
                try
                {
                    itemKey = value;
                    itemType = "Uncategorized";
                    itemName = itemKey.Substring(itemKey.LastIndexOf('/') + 1);
                    if (itemKey.Contains("/Weapons/"))
                    {
                        itemType = "Weapon";
                        if (itemName.Contains("Mod_")) itemName = itemName.Replace("/Weapons/", "/Mods/");
                        if (itemName.Contains("Wasteland_Flail")) itemName = itemName.Replace("_Flail", "_WastelanderFlail");
                    }
                    if (itemKey.Contains("/Armor/") || itemKey.Contains("TwistedMask"))
                    {
                        itemType = "Armor";
                        if (itemKey.Contains("TwistedMask"))
                        {
                            itemName = "TwistedMask (Head)";
                        }
                        else
                        {
                            string[] parts = itemName.Split('_');
                            itemName = parts[2] + " (" + parts[1] + ")";
                        }
                    }
                    if (itemName.Contains("Sumpremacy"))
                    {
                        itemName = itemName.Replace("Sumpremacy", "Supremacy");
                    }
                    if (itemName.Contains("GIft")) itemName = itemName.Replace("GIft", "Gift");
                    if (itemKey.Contains("/Trinkets/") || itemKey.Contains("BrabusPocketWatch")) itemType = "Trinket";
                    if (itemKey.Contains("/Mods/")) itemType = "Mod";
                    if (itemKey.Contains("/Traits/")) itemType = "Trait";
                    if (itemKey.Contains("/Emotes/")) itemType = "Emote";

                    itemName = itemName.Replace("Weapon_", "").Replace("Root_", "").Replace("Wasteland_", "").Replace("Swamp_", "").Replace("Pan_", "").Replace("Atoll_", "").Replace("Mod_", "").Replace("Trinket_", "").Replace("Trait_", "").Replace("Quest_", "").Replace("Emote_", "");
                    if (!itemType.Equals("Armor"))
                    {
                        itemName = Regex.Replace(itemName, "([a-z])([A-Z])", "$1 $2");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error processing item name: " + ex.Message);
                    itemName = value;
                }
            } 
        }

        public string ItemName { get { return itemName; } }
        public string ItemType { get { return itemType; } }
        public RemnantItemMode ItemMode { get; set; }
        public string ItemNotes { get; set; }

        public RemnantItem(string key)
        {
            this.ItemKey = key;
            this.ItemMode = RemnantItemMode.Normal;
            this.ItemNotes = "";
        }

        public RemnantItem(string key, RemnantItemMode mode)
        {
            this.ItemKey = key;
            this.ItemMode = mode;
            this.ItemNotes = "";
        }

        public string GetKey()
        {
            return this.itemKey;
        }

        public override string ToString()
        {
            /*string name = itemKey.Substring(itemKey.LastIndexOf('/') + 1);
            string prefix = "";
            if (itemKey.Contains("/Weapons/"))
            {
                prefix = "Weapon: ";
                if (name.Contains("Mod_")) name = name.Replace("/Weapons/", "/Mods/");
                if (name.Contains("Wasteland_Flail")) name = name.Replace("_Flail", "_WastelanderFlail");
            }
            if (itemKey.Contains("/Armor/") || itemKey.Contains("TwistedMask"))
            {
                prefix = "Armor: ";
                if (itemKey.Contains("TwistedMask"))
                {
                    name = "TwistedMask (Head)";
                }
                else
                {
                    string[] parts = name.Split('_');
                    name = parts[2] + " (" + parts[1] + ")";
                }
            }
            if (name.Contains("Sumpremacy"))
            {
                name = name.Replace("Sumpremacy", "Supremacy");
            }
            if (name.Contains("GIft")) name = name.Replace("GIft", "Gift");
            if (itemKey.Contains("/Trinkets/")) prefix = "Trinket: ";
            if (itemKey.Contains("/Mods/")) prefix = "Mod: ";
            if (itemKey.Contains("/Traits/")) prefix = "Trait: ";
            if (itemKey.Contains("/Emotes/")) prefix = "Emote: ";

            name = name.Replace("Weapon_", "").Replace("Root_", "").Replace("Wasteland_", "").Replace("Swamp_", "").Replace("Pan_", "").Replace("Atoll_", "").Replace("Mod_", "").Replace("Trinket_", "").Replace("Trait_", "").Replace("Quest_", "").Replace("Emote_", "");
            if (!prefix.Equals("Armor: "))
            {
                name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
            }
            if (this.ItemMode == RemnantItemMode.Hardcore)
            {
                name += " (HC)";
            } 
            else if (this.ItemMode == RemnantItemMode.Survival)
            {
                name += " (S)";
            }
            return prefix + name;*/
            return itemType + ": " + itemName;
        }

        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null))
            {
                return false;
            }
            else if (!this.GetType().Equals(obj.GetType()))
            {
                if (obj.GetType() == typeof(string))
                {
                    return (this.GetKey().Equals(obj));
                }
                return false;
            }
            else
            {
                RemnantItem rItem = (RemnantItem)obj;
                return (this.GetKey().Equals(rItem.GetKey()) && this.ItemMode == rItem.ItemMode);
            }
        }

        public override int GetHashCode()
        {
            return this.itemKey.GetHashCode();
        }

        public int CompareTo(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null))
            {
                return 1;
            }
            else if (!this.GetType().Equals(obj.GetType()))
            {
                if (obj.GetType() == typeof(string))
                {
                    return (this.GetKey().CompareTo(obj));
                }
                return this.ToString().CompareTo(obj.ToString());
            }
            else
            {
                RemnantItem rItem = (RemnantItem)obj;
                if (this.ItemMode != rItem.ItemMode)
                {
                    return this.ItemMode.CompareTo(rItem.ItemMode);
                }
                return this.itemKey.CompareTo(rItem.GetKey());
            }
        }
    }
}
