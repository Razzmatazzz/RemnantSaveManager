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
            if (itemKey.Contains("/Trinkets/")) prefix = "Trinket: ";
            if (itemKey.Contains("/Mods/")) prefix = "Mod: ";
            if (itemKey.Contains("/Traits/")) prefix = "Trait: ";

            name = name.Replace("Weapon_", "").Replace("Root_", "").Replace("Wasteland_", "").Replace("Swamp_", "").Replace("Pan_", "").Replace("Atoll_", "").Replace("Mod_", "").Replace("Trinket_", "").Replace("Trait_", "").Replace("Quest_", "");
            if (!prefix.Equals("Armor: "))
            {
                name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
            }
            return prefix + name;
        }

        /*private static Dictionary<string, string[]> eventItem = new Dictionary<string, string[]>();
        public static Dictionary<string, string[]> EventItem
        {
            get
            {
                if (eventItem.Count == 0)
                {
                    RefreshEventItems();
                }

                return eventItem;
            }
        }

        public static void RefreshEventItems()
        {
            eventItem.Clear();
            string eventName = null;
            List<string> items = new List<string>();
            XmlTextReader reader = new XmlTextReader("Resources\\EventItem.xml");
            reader.WhitespaceHandling = WhitespaceHandling.None;
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name.Equals("Event"))
                        {
                            eventName = reader.GetAttribute("name");
                        }
                        else if (reader.Name.Equals("Item"))
                        {
                            //do anything?
                        }
                        break;
                    case XmlNodeType.Text:
                        items.Add(reader.Value);
                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name.Equals("Event"))
                        {
                            eventItem.Add(eventName, items.ToArray());
                            eventName = null;
                            items.Clear();
                        }
                        break;
                }
            }
            reader.Close();
        }

        public static string CheckForNewEventItems()
        {
            string message = "No new event items found.";
            XmlTextReader reader = new XmlTextReader("https://raw.githubusercontent.com/Razzmatazzz/RemnantSaveManager/master/Resources/EventItem.xml");
            reader.WhitespaceHandling = WhitespaceHandling.None;
            int remoteversion = 0;
            int localversion = 0;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name.Equals("EventItems"))
                    {
                        remoteversion = int.Parse(reader.GetAttribute("version"));
                        break;
                    }
                }
            }
            reader.Close();
            reader = new XmlTextReader("Resources\\EventItem.xml");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name.Equals("EventItems"))
                    {
                        localversion = int.Parse(reader.GetAttribute("version"));
                        break;
                    }
                }
            }
            reader.Close();

            if (remoteversion > localversion)
            {
                WebClient client = new WebClient();
                client.DownloadFile("https://raw.githubusercontent.com/Razzmatazzz/RemnantSaveManager/master/Resources/EventItem.xml", "Resources\\EventItem.xml");
                RefreshEventItems();
                message = "Event items updated!";
            }

            return message;
        }*/

        /*public static Dictionary<string, string[]> EventItem = new Dictionary<string, string[]>(){
            { "AcesCoin", new string[] {"/Items/Weapons/Basic/HandGuns/Revolver/Weapon_Revolver" } },
            { "ArmorVault", new string[] {"/Items/Armor/Akari/Armor_Head_Akari", "/Items/Armor/Akari/Armor_Body_Akari", "/Items/Armor/Akari/Armor_Legs_Akari" } },
            { "BandOfStrength", new string[] {"/Items/Trinkets/BandOfStrength" } },
            { "BlinkFiend", new string[] {"/Items/Mods/BlinkToken" } },
            { "BlinkThief", new string[] {"/Items/Weapons/Basic/RicochetRifle/Weapon_Pan_RicochetRifle", "/Items/Trinkets/Trinket_CelerityStone" } },
            { "BloodFont", new string[] {"/Items/Trinkets/Trinket_BloodFont" } },
            { "Brabus", new string[] {"/Items/Mods/ExplosiveShot", "/Items/Armor/Bandit/Armor_Head_Bandit", "/Items/Armor/Bandit/Armor_Body_Bandit", "/Items/Armor/Bandit/Armor_Legs_Bandit", "/Items/Traits/Trait_ColdAsIce" } },
            { "BrutalMark", new string[] {"/Items/Trinkets/BrutalMark" } },
            { "ButchersFetish", new string[] {"/Items/Trinkets/ButchersFetish" } },
            { "CleansingJewel", new string[] {"/Items/Trinkets/CleansingJewel" } },
            { "DevouringLoop", new string[] {"/Items/Trinkets/DevouringLoop" } },
            { "DoeShrine", new string[] {"/Items/Trinkets/Trinket_ScavengersBauble", "/Items/Traits/Trait_Swiftness" } },
            { "DrifterMask", new string[] {"/Items/Armor/Drifter/Armor_Head_Drifter" } },
            { "EzlansBand", new string[] {"/Items/Trinkets/Trinket_EzlansBand" } },
            { "Fatty", new string[] {"/Items/Weapons/Boss/Devastator/Weapon_Swamp_Devastator", "/Items/Traits/Trait_Glutton" } },
            { "Flautist", new string[] {"/Items/Trinkets/Trinket_HeartOfTheWolf", "/Items/Traits/Trait_Swiftness" } },
            { "FoundersHideout", new string[] { "/Items/Armor/Drifter/Armor_Body_Drifter", "/Items/Armor/Drifter/Armor_Legs_Drifter" } },
            { "GalenicCharm", new string[] {"/Items/Trinkets/Trinket_GalenicCharm" } },
            { "GravityStone", new string[] {"/Items/Trinkets/GravityStone" } },
            { "GunslingersCharm", new string[] {"/Items/Trinkets/Trinket_GunslingersCharm" } },
            { "HeartSeeker", new string[] {"/Items/Trinkets/HeartSeeker" } },
            { "HoundMaster", new string[] {"/Items/Mods/HowlersImmunity" } },
            { "HuntersBand", new string[] {"/Items/Trinkets/HuntersBand" } },
            { "HuntersHalo", new string[] {"/Items/Trinkets/Trinket_HuntersHalo" } },
            { "HuntersHideout", new string[] {"/Items/Weapons/Basic/HandGuns/HuntingPistol/Weapon_HuntingPistol", "/Items/Traits/Trait_ShadowWalker" } },
            { "ImmolatorAndZephyr", new string[] {"/Items/Mods/WildfireShot" } },
            { "KeepersRing", new string[] {"/Items/Trinkets/Trinket_KeepersRing" } },
            { "KinCaller", new string[] {"/Items/Mods/SongOfSwords" } },
            { "LastWill", new string[] {"/Items/Weapons/Basic/LongGuns/AssaultRifle/Weapon_AssaultRifle", "/Items/Traits/Trait_Spirit" } },
            { "LeechEmber", new string[] {"/Items/Trinkets/Trinket_LeechEmber" } },
            { "LizAndLiz", new string[] {"/Items/Weapons/Basic/LongGuns/MachineGun/Weapon_Machinegun", "/Items/Traits/Trait_Warrior" } },
            { "MadMerchant", new string[] {"/Items/QuestItems/TwistedMask/Quest_TwistedMask" } },
            { "Monolith", new string[] { "/Items/Armor/Void/Armor_Head_Void", "/Items/Armor/Void/Armor_Body_Void", "/Items/Armor/Void/Armor_Legs_Void" } },
            { "MothersRing", new string[] {"/Items/Trinkets/Trinket_MothersRing" } },
            { "MudTooth", new string[] {"/Quests/Quest_OverworldPOI_MudTooth/Quest_BrabusPocketWatch" } },
            { "OldManAndConstruct", new string[] {"/Items/Trinkets/JewelOfTheBlackSun", "/Items/Trinkets/MendersCharm", "/Items/Armor/Osseous/Armor_Head_Osseous", "/Items/Armor/Osseous/Armor_Body_Osseous", "/Items/Armor/Osseous/Armor_Legs_Osseous", "/Items/Mods/IronSentinel" } },
            { "Penitent", new string[] {"/Items/Trinkets/Trinket_LetosAmulet" } },
            { "PillarOfStone", new string[] {"/Items/Trinkets/Trinket_PillarOfStone" } },
            { "RazorStone", new string[] {"/Items/Trinkets/Razorstone" } },
            { "ReggiesRing", new string[] {"/Items/Traits/Trait_Scavenger" } },
            { "RingOfEvasion", new string[] {"/Items/Trinkets/Trinket_RingOfEvasion" } },
            { "RockOfAnguish", new string[] {"/Items/Trinkets/RockOfAnguish" } },
            { "RootBrute", new string[] {"/Items/Mods/MantleOfThorns" } },
            { "RootCultist", new string[] { "/Items/Trinkets/BraidedThorns", "/Items/Trinkets/Trinket_RootCirclet" } },
            { "RootDragon", new string[] {"/Items/Weapons/Boss/Root_Spitfire/Weapon_Root_Spitfire", "/Items/Weapons/Boss/Root_Smolder/Weapon_Root_Smolder" } },
            { "RootTumbleweed", new string[] {"/Items/Mods/SeedCaller" } },
            { "RootWraith", new string[] {"/Items/Mods/Rattleweed" } },
            { "RootEnt", new string[] { "/Items/Weapons/Boss/Root_SporeLauncher/Weapon_Root_SporeLauncher", "/Items/Weapons/Boss/Root_PetrifiedMaul/Weapon_Root_PetrifiedMaul", "/Items/Traits/Trait_QuickHands" } },
            { "RootShrine", new string[] {"/Items/Armor/Twisted/Armor_Head_Twisted", "/Items/Armor/Twisted/Armor_Body_Twisted", "/Items/Armor/Twisted/Armor_Legs_Twisted" } },
            { "Sagestone", new string[] {"/Items/Trinkets/Trinket_Sagestone" } },
            { "Sentinel", new string[] {"/Items/Mods/Beckon" } },
            { "SlimeHulk", new string[] {"/Items/Mods/CorrosiveAura", "/Items/Traits/Trait_Catalyst" } },
            { "Splitter", new string[] {"/Items/Mods/FlickerCloak" } },
            { "StoneOfBalance", new string[] {"/Items/Trinkets/Trinket_StoneOfBalance" } },
            { "StormAmulet", new string[] {"/Items/Trinkets/Trinket_StormAmulet" } },
            { "StormCaller", new string[] {"/Items/Mods/StormCaller" } },
            { "StuckMerchant", new string[] {"/Items/Weapons/Basic/Spear/Weapon_Pan_Spear", "/Items/Armor/Radiant/Armor_Legs_Radiant", "/Items/Armor/Radiant/Armor_Body_Radiant", "/Items/Armor/Radiant/Armor_Head_Radiant", "/Items/Trinkets/Trinket_GuardiansRing", "/Items/Traits/Trait_GuardiansBlessing" } },
            { "SwampGuardian", new string[] {"/Items/Weapons/Boss/HiveCannon/Weapon_Swamp_HiveCannon", "/Items/Weapons/Boss/GuardianAxe/Weapon_Swamp_GuardianAxe", "/Items/Traits/Trait_Executioner" } },
            { "SwarmMaster", new string[] {"/Items/Mods/BreathOfDesert" } },
            { "TheCleanRoom", new string[] {"/Items/Weapons/Basic/Wasteland_Flail/Weapon_Wasteland_Flail" } },
            { "TheHarrow", new string[] { "/Items/Weapons/Boss/Defiler/Weapon_Wasteland_Defiler", "/Items/Weapons/Boss/LostHarpoon/Weapon_Wasteland_LostHarpoon" } },
            { "TheLostGantry", new string[] {"/Items/Weapons/Basic/Wasteland_BeamRifle/Weapon_Wasteland_BeamRifle" } },
            { "TheRisen", new string[] {"/Items/Trinkets/SoulAnchor" } },
            { "TotemFather", new string[] {"/Items/Traits/Trait_ArcaneStrike", "/Items/Weapons/Boss/Pan_EyeOfTheStorm/Weapon_Pan_EyeOfTheStorm", "/Items/Weapons/Boss/Pan_VoiceOfTheTempest/Weapon_Pan_VoiceOfTheTempest" } },
            { "Tyrant", new string[] {"/Items/Mods/Swarm", "/Items/Traits/Trait_Catalyst" } },
            { "UndyingKing", new string[] {"/Items/Weapons/Boss/Ruin/Weapon_Wasteland_Ruin", "/Items/Traits/Trait_Kingslayer", "/Items/Weapons/Boss/Riven/Weapon_Wasteland_Riven" } },
            { "VengeanceIdol", new string[] {"/Items/Trinkets/VengeanceIdol" } },
            { "Vyr", new string[] {"/Items/Mods/VeilOfTheBlackTear" } },
            { "WailingWood", new string[] {"/Items/Trinkets/Trinket_TwistedIdol", "/Items/Traits/Trait_BarkSkin" } },
            { "Ward13", new string[] { "/Items/Weapons/Basic/LongGuns/Coachgun/Weapon_Coachgun", "/Items/Weapons/Human/Melee/Hatchet/Weapon_Hatchet", "/Items/Armor/Cultist/Armor_Body_Cultist", "/Items/Armor/Cultist/Armor_Head_Cultist", "/Items/Armor/Cultist/Armor_Legs_Cultist", "/Items/Weapons/Basic/LongGuns/HuntingRifle/Weapon_HuntingRifle", "/Items/Weapons/Human/Melee/Sword/Weapon_Sword", "/Items/Armor/Hunter/Armor_Body_Hunter", "/Items/Armor/Hunter/Armor_Head_Hunter", "/Items/Armor/Hunter/Armor_Legs_Hunter", "/Items/Weapons/Basic/LongGuns/Shotgun/Weapon_Shotgun", "/Items/Armor/Scrapper/Armor_Body_Scrapper", "/Items/Armor/Scrapper/Armor_Head_Scrapper", "/Items/Armor/Scrapper/Armor_Legs_Scrapper", "/Items/Weapons/Human/Melee/Hammer/Weapon_Hammer", "/Items/Traits/Trait_ElderKnowledge", "/Items/Weapons/Basic/HandGuns/SubMachineGun/Weapon_Submachinegun", "/Items/Trinkets/RingOfTheAdmiral" } },
            { "Ward17", new string[] { "/Items/Weapons/Boss/Guns/LongGuns/Repulsor/Weapon_Atoll_Repulsor", "/Items/Traits/Trait_MindsEye" } },
            { "WastelandGuardian", new string[] {"/Items/Weapons/Boss/WorldBreaker/Weapon_Wasteland_WorldBreaker", "/Items/Weapons/Boss/ParticleAccelerator/Weapon_Wasteland_ParticleAccelerator", "/Items/Traits/Trait_Recovery" } },
            { "Wolf", new string[] {"/Items/Weapons/Boss/Pan_CurseOfTheJungleGod/Weapon_Pan_CurseOfTheJungleGod", "/Items/Weapons/Boss/Pan_ScarOfTheJungleGod/Weapon_Pan_ScarOfTheJungleGod", "/Items/Traits/Trait_ArcaneStrike", "/Items/Traits/Trait_Swiftness" } },
            { "WolfShrine", new string[] {"/Items/Armor/Elder/Armor_Head_Elder", "/Items/Armor/Elder/Armor_Body_Elder", "/Items/Armor/Elder/Armor_Legs_Elder" } }
        };*/
    }
}
