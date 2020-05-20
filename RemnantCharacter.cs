using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace RemnantSaveManager
{
    public class RemnantCharacter
    {
        public string Archetype { get; set; }
        public List<string> Inventory { get; set; }
        public List<RemnantWorldEvent> CampaignEvents { get; set; }
        public List<RemnantWorldEvent> AdventureEvents { get; set; }

        public int Progression {
            get {
                return this.Inventory.Count;
            }
        }

        private List<RemnantItem> missingItems;

        private string savePath;

        public enum ProcessMode { Campaign, Adventure };

        public override string ToString()
        {
            return this.Archetype + " (" + this.Progression + ")";
        }

        public string ToFullString()
        {
            string str = "CharacterData{ Archetype: " + this.Archetype + ", Inventory: [" + string.Join(", ", this.Inventory) + "], CampaignEvents: [" + string.Join(", ", this.CampaignEvents) + "], AdventureEvents: [" + string.Join(", ", this.AdventureEvents) + "] }";
            return str;
        }

        public RemnantCharacter()
        {
            this.Archetype = "";
            this.Inventory = new List<string>();
            this.CampaignEvents = new List<RemnantWorldEvent>();
            this.AdventureEvents = new List<RemnantWorldEvent>();
            this.missingItems = new List<RemnantItem>();
            this.savePath = null;
        }

        public void processSaveData(string savetext)
        {
            //get campaign info
            string strCmpaignEnd = "/Game/Campaign_Main/Quest_Campaign_Main.Quest_Campaign_Main_C";
            string strCampaignStart = "/Game/Campaign_Main/Quest_Campaign_City.Quest_Campaign_City";
            int campaignEnd = savetext.IndexOf(strCmpaignEnd);
            int campaignStart = savetext.IndexOf(strCampaignStart);
            if (campaignStart != -1 && campaignEnd != -1)
            {
                string campaigntext = savetext.Substring(0, campaignEnd);
                campaignStart = campaigntext.LastIndexOf(strCampaignStart);
                campaigntext = campaigntext.Substring(campaignStart);
                RemnantWorldEvent.ProcessEvents(this, campaigntext, RemnantWorldEvent.ProcessMode.Campaign);
            }
            else
            {
                Console.WriteLine("Campaign not found; likely in tutorial mission.");
            }

            //get adventure info
            if (savetext.Contains("Quest_AdventureMode_"))
            {
                string adventureZone = null;
                if (savetext.Contains("Quest_AdventureMode_City_C")) adventureZone = "City";
                if (savetext.Contains("Quest_AdventureMode_Wasteland_C")) adventureZone = "Wasteland";
                if (savetext.Contains("Quest_AdventureMode_Swamp_C")) adventureZone = "Swamp";
                if (savetext.Contains("Quest_AdventureMode_Jungle_C")) adventureZone = "Jungle";

                string strAdventureEnd = String.Format("/Game/World_{0}/Quests/Quest_AdventureMode/Quest_AdventureMode_{0}.Quest_AdventureMode_{0}_C", adventureZone);
                int adventureEnd = savetext.IndexOf(strAdventureEnd) + strAdventureEnd.Length;
                string advtext = savetext.Substring(0, adventureEnd);
                string strAdventureStart = String.Format("/Game/World_{0}/Quests/Quest_AdventureMode/Quest_AdventureMode_{0}_0", adventureZone);
                int adventureStart = advtext.LastIndexOf(strAdventureStart) + strAdventureStart.Length;
                //advtext = advtext.Substring(advtext.LastIndexOf("Quest_Campaign_Main_C"));
                advtext = advtext.Substring(adventureStart);
                RemnantWorldEvent.ProcessEvents(this, advtext, RemnantWorldEvent.ProcessMode.Adventure);
            }

            missingItems.Clear();
            foreach (RemnantItem[] eventItems in GameInfo.EventItem.Values)
            {
                foreach (RemnantItem item in eventItems)
                {
                    if (!this.Inventory.Contains(item.GetKey()))
                    {
                        if (!missingItems.Contains(item))
                        {
                            missingItems.Add(item);
                        }
                    }
                }
            }
            missingItems.Sort();
        }

        public enum CharacterProcessingMode { All, NoEvents };

        public static List<RemnantCharacter> GetCharactersFromSave(string saveFolderPath)
        {
            return GetCharactersFromSave(saveFolderPath, CharacterProcessingMode.All);
        }

        public static List<RemnantCharacter> GetCharactersFromSave(string saveFolderPath, CharacterProcessingMode mode)
        {
            List<RemnantCharacter> charData = new List<RemnantCharacter>();
            try
            {
                string profileData = File.ReadAllText(saveFolderPath + "\\profile.sav");
                MatchCollection archetypes = new Regex(@"/Game/_Core/Archetypes/[a-zA-Z_]+").Matches(profileData);
                for (int i = 0; i < archetypes.Count; i++)
                {
                    RemnantCharacter cd = new RemnantCharacter();
                    cd.Archetype = archetypes[i].Value.Replace("/Game/_Core/Archetypes/", "").Split('_')[1];
                    cd.savePath = saveFolderPath;
                    charData.Add(cd);
                }

                string[] inventories = profileData.Split(new string[] { "/Game/_Core/Archetypes/" }, StringSplitOptions.None);
                for (var i = 1; i < inventories.Length; i++)
                {
                    if (inventories[i].IndexOf("/Game/Characters/Player/Base/Character_Master_Player.Character_Master_Player_C") == -1)
                    {
                        continue;
                    }
                    List<string> saveItems = new List<string>();
                    string charStart = "/Game/Characters/Player/Base/Character_Master_Player.Character_Master_Player_C";
                    string charEnd = "Character_Master_Player_C";
                    inventories[i] = inventories[i].Substring(inventories[i].IndexOf(charStart)+charStart.Length);
                    inventories[i] = inventories[i].Substring(0, inventories[i].IndexOf(charEnd));
                    Regex rx = new Regex(@"/Items/Weapons(/[a-zA-Z0-9_]+)+/[a-zA-Z0-9_]+");
                    MatchCollection matches = rx.Matches(inventories[i]);
                    foreach (Match match in matches)
                    {
                        saveItems.Add(match.Value);
                    }

                    //rx = new Regex(@"/Items/Armor/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+");
                    rx = new Regex(@"/Items/Armor/([a-zA-Z0-9_]+/)?[a-zA-Z0-9_]+");
                    matches = rx.Matches(inventories[i]);
                    foreach (Match match in matches)
                    {
                        saveItems.Add(match.Value);
                    }

                    rx = new Regex(@"/Items/Trinkets/(BandsOfCastorAndPollux/)?[a-zA-Z0-9_]+");
                    matches = rx.Matches(inventories[i]);
                    foreach (Match match in matches)
                    {
                        saveItems.Add(match.Value);
                    }

                    rx = new Regex(@"/Items/Mods/[a-zA-Z0-9_]+");
                    matches = rx.Matches(inventories[i]);
                    foreach (Match match in matches)
                    {
                        saveItems.Add(match.Value);
                    }

                    rx = new Regex(@"/Items/Traits/[a-zA-Z0-9_]+");
                    matches = rx.Matches(inventories[i]);
                    foreach (Match match in matches)
                    {
                        saveItems.Add(match.Value);
                    }

                    rx = new Regex(@"/Items/QuestItems(/[a-zA-Z0-9_]+)+/[a-zA-Z0-9_]+");
                    matches = rx.Matches(inventories[i]);
                    foreach (Match match in matches)
                    {
                        saveItems.Add(match.Value);
                    }

                    rx = new Regex(@"/Quests/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+");
                    matches = rx.Matches(inventories[i]);
                    foreach (Match match in matches)
                    {
                        saveItems.Add(match.Value);
                    }

                    rx = new Regex(@"/Player/Emotes/Emote_[a-zA-Z0-9]+");
                    matches = rx.Matches(inventories[i]);
                    foreach (Match match in matches)
                    {
                        saveItems.Add(match.Value);
                    }
                    charData[i - 1].Inventory = saveItems;
                }

                if (mode == CharacterProcessingMode.All)
                {
                    string[] saves = Directory.GetFiles(saveFolderPath, "save_*.sav");
                    for (int i = 0; i < saves.Length && i < charData.Count; i++)
                    {
                        charData[i].processSaveData(File.ReadAllText(saves[i]));
                    }
                }
            }
            catch (IOException ex)
            {
                if (ex.Message.Contains("being used by another process"))
                {
                    Console.WriteLine("Save file in use; waiting 0.5 seconds and retrying.");
                    System.Threading.Thread.Sleep(500);
                    charData = GetCharactersFromSave(saveFolderPath, mode);
                }
            }
            return charData;
        }

        public void LoadWorldData(int charIndex)
        {
            if (this.savePath != null)
            {
                if (this.CampaignEvents.Count == 0)
                {
                    string[] saves = Directory.GetFiles(this.savePath, "save_*.sav");
                    if (charIndex < saves.Length)
                    {
                        try
                        {
                            this.processSaveData(File.ReadAllText(saves[charIndex]));
                        }
                        catch (IOException ex)
                        {
                            if (ex.Message.Contains("being used by another process"))
                            {
                                Console.WriteLine("Save file in use; waiting 0.5 seconds and retrying.");
                                System.Threading.Thread.Sleep(500);
                                LoadWorldData(charIndex);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error loading world Data: ");
                            Console.WriteLine("\tCharacterData.LoadWorldData");
                            Console.WriteLine("\t"+ex.ToString());
                        }
                    }
                }
            }
        }

        public List<RemnantItem> GetMissingItems()
        {
            return missingItems;
        }
    }
}
