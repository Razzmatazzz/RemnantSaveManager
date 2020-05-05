using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Xml;
using System.Net;

namespace RemnantSaveManager
{
    public class RemnantWorldEvent
    {
        private string eventKey;
        private List<string> mItems;
        public string Location { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string MissingItems {
            get {
                return string.Join("\n", mItems);
            }
        }


        public string getKey()
        {
            return eventKey;
        }

        public void setKey(string key)
        {
            this.eventKey = key;
        }

        public List<string> getPossibleItems()
        {
            List<string> items = new List<string>();
            if (GameInfo.EventItem.ContainsKey(this.eventKey))
            {
                string[] arrItems = GameInfo.EventItem[this.eventKey];
                for (int i=0; i < arrItems.Length; i++)
                {
                    items.Add(arrItems[i]);
                }
            }
            return items;
        }

        public void setMissingItems(RemnantCharacter charData)
        {
            List<string> missingItems = new List<string>();
            List<string> possibleItems = this.getPossibleItems();
            foreach (string item in possibleItems)
            {

                if (!charData.Inventory.Contains(item))
                {
                    missingItems.Add(GetCleanItemName(item));
                }
            }
            if (this.eventKey.Equals("Sketterling"))
            {
                missingItems.Add("Unknown. May indicate 100% chance spawn");
                missingItems.Add("of Timid Beetle in previous dungeon.");
            }
            mItems = missingItems;
        }

        public override string ToString()
        {
            return this.Name;
        }

       public static string GetCleanItemName(string itemKey)
        {
            string name = itemKey.Substring(itemKey.LastIndexOf('/') + 1);
            string prefix = "";
            if (itemKey.Contains("/Weapons/"))
            {
                prefix = "Weapon: ";
                if (name.Contains("Mod_")) itemKey.Replace("/Weapons/", "/Mods/");
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
            return prefix + name;
        }
    }
}
