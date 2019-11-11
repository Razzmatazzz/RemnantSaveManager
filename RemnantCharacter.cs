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

        private List<string> missingItems;

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
            this.missingItems = new List<string>();
            this.savePath = null;
        }

        public void processSaveData(string savetext)
        {
            //get adventure info
            string adventureZone = null;
            if (savetext.Contains("Quest_AdventureMode_City")) adventureZone = "City";
            if (savetext.Contains("Quest_AdventureMode_Wasteland")) adventureZone = "Wasteland";
            if (savetext.Contains("Quest_AdventureMode_Swamp")) adventureZone = "Swamp";
            if (savetext.Contains("Quest_AdventureMode_Jungle")) adventureZone = "Jungle";

            string strCampaignEnd = "/Game/Campaign_Main/Quest_Campaign_Ward13.Quest_Campaign_Ward13";
            int campaignEnd = savetext.IndexOf(strCampaignEnd)+strCampaignEnd.Length;
            int eventsEnd = campaignEnd;

            if (adventureZone != null)
            {
                string strAdventureEnd = String.Format("/Game/World_{0}/Quests/Quest_AdventureMode/Quest_AdventureMode_{0}.Quest_AdventureMode_{0}_C", adventureZone);
                int adventureEnd = savetext.IndexOf(strAdventureEnd) + strAdventureEnd.Length;
                if (adventureEnd > campaignEnd)
                {
                    savetext = savetext.Substring(0, adventureEnd);
                } else
                {
                    savetext = savetext.Substring(0, campaignEnd);
                }
                string adventuretext = savetext.Split(new string[] { strAdventureEnd }, StringSplitOptions.None)[0];
                adventuretext = adventuretext.Split(new string[] { String.Format("/Game/World_{0}/Quests/Quest_AdventureMode/Quest_AdventureMode_{0}_0", adventureZone) }, StringSplitOptions.None)[1];
                adventuretext = adventuretext.Split(new string[] { "PersistenceKeys" }, StringSplitOptions.None)[0];
                adventuretext = adventuretext.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0].Split(new string[] { "\n" }, StringSplitOptions.None)[0];
                processEvents(adventuretext, ProcessMode.Adventure);
            }

            //get campaign info
            savetext = savetext.Split(new string[] { "/Game/Campaign_Main/Quest_Campaign_Ward13.Quest_Campaign_Ward13" }, StringSplitOptions.None)[0];
            string[] campaign = savetext.Split(new string[] { "/Game/Campaign_Main/Quest_Campaign_City.Quest_Campaign_City" }, StringSplitOptions.None);
            if (campaign.Length > 1)
            {
                savetext = campaign[1];
                processEvents(savetext, ProcessMode.Campaign);
            } else
            {
                Console.WriteLine("Campaign not found; likely in tutorial mission.");
            }

            missingItems.Clear();
            foreach (string[] eventItems in GameInfo.EventItem.Values)
            {
                foreach (string item in eventItems)
                {
                    if (!this.Inventory.Contains(item))
                    {
                        missingItems.Add(RemnantWorldEvent.GetCleanItemName(item));
                    }
                }
            }
            missingItems.Sort();
        }

        //credit to /u/hzla00 for original javascript implementation
        private void processEvents(string eventsText, ProcessMode mode)
        {
            Dictionary<string, Dictionary<string, string>> zones = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, List<RemnantWorldEvent>> zoneEvents = new Dictionary<string, List<RemnantWorldEvent>>();
            foreach (string z in GameInfo.Zones)
            {
                zones.Add(z, new Dictionary<string, string>());
                zoneEvents.Add(z, new List<RemnantWorldEvent>());
            }

            string currentMainLocation = "Fairview";
            string currentSublocation = null;

            string eventName = null;
            MatchCollection matches = Regex.Matches(eventsText, "(?:/[a-zA-Z0-9_]+){3}/([a-zA-Z0-9]+_[a-zA-Z0-9]+_[a-zA-Z0-9_]+)");
            foreach (Match match in matches)
            {
                RemnantWorldEvent se = new RemnantWorldEvent();
                string zone = null;
                string eventType = null;
                string lastEventname = eventName;
                eventName = null;

                string textLine = match.Value;
                try
                {
                    if (currentSublocation != null) {
                        if (currentSublocation.Equals("TheRavagersHaunt") || currentSublocation.Equals("TheTempestCourt")) currentSublocation = null;
                    }
                    zone = getZone(textLine);

                    eventType = getEventType(textLine);
                    if (textLine.Contains("Overworld_Zone"))
                    {
                        currentMainLocation = textLine.Split('/')[4].Split('_')[1] + " " + textLine.Split('/')[4].Split('_')[2] + " " + textLine.Split('/')[4].Split('_')[3];
                        if (GameInfo.MainLocations.ContainsKey(currentMainLocation))
                        {
                            currentMainLocation = GameInfo.MainLocations[currentMainLocation];
                        }
                        else
                        {
                            currentMainLocation = null;
                        }
                    }
                    else if (eventType != null)
                    {
                        eventName = textLine.Split('/')[4].Split('_')[2];
                        if (textLine.Contains("OverworldPOI"))
                        {
                            currentSublocation = null;
                        } else if (!textLine.Contains("Quest_Event"))
                        {
                            if (GameInfo.SubLocations.ContainsKey(eventName))
                            {
                                currentSublocation = GameInfo.SubLocations[eventName];
                            }
                            else
                            {
                                currentSublocation = null;
                            }
                        }
                    }

                    if (mode == ProcessMode.Adventure) currentMainLocation = null;
                    
                    if (eventName != lastEventname)
                    {
                        // Replacements
                        if (eventName != null)
                        {
                            se.setKey(eventName);
                            se.Name = eventName.Replace("LizAndLiz", "TaleOfTwoLiz's").Replace("Fatty", "TheUncleanOne").Replace("WastelandGuardian", "Claviger").Replace("RootEnt", "TheEnt").Replace("Wolf", "TheRavager").Replace("RootDragon", "Singe").Replace("SwarmMaster", "Scourge").Replace("RootWraith", "Shroud").Replace("RootTumbleweed", "TheMangler").Replace("Kincaller", "Warden").Replace("Tyrant", "Thrall").Replace("Vyr", "ShadeAndShatter").Replace("ImmolatorAndZephyr", "ScaldAndSear").Replace("RootBrute", "Gorefist").Replace("SlimeHulk", "Canker").Replace("BlinkFiend", "Onslaught").Replace("Sentinel", "Raze").Replace("Penitent", "Leto'sAmulet").Replace("LastWill", "SupplyRun").Replace("SwampGuardian", "Ixillis").Replace("OldManAndConstruct", "WudAndAncientConstruct").Replace("Splitter","Riphide").Replace("Nexus", "RootNexus");
                            se.Name = Regex.Replace(se.Name, "([a-z])([A-Z])", "$1 $2");
                        }

                        if (zone != null && eventType != null && eventName != null)
                        {
                            if (!zones[zone].ContainsKey(eventType))
                            {
                                zones[zone].Add(eventType, "");
                            }
                            if (!zones[zone][eventType].Contains(eventName))
                            {
                                zones[zone][eventType] += ", " + eventName;
                                List<string> locationList = new List<string>();
                                locationList.Add(zone);
                                if (currentMainLocation != null) locationList.Add(Regex.Replace(currentMainLocation, "([a-z])([A-Z])", "$1 $2"));
                                if (currentSublocation != null) locationList.Add(Regex.Replace(currentSublocation, "([a-z])([A-Z])", "$1 $2"));
                                se.Location = string.Join(": ", locationList);
                                se.Type = eventType;
                                se.setMissingItems(this);
                                zoneEvents[zone].Add(se);
                            }
                        }

                    }
                } catch (Exception ex)
                {
                    Console.WriteLine("Error parsing save event:");
                    Console.WriteLine("\tLine: "+textLine);
                    Console.WriteLine("\tError: " + ex.ToString());
                }
            }

            List<RemnantWorldEvent> orderedEvents = new List<RemnantWorldEvent>();

            bool churchAdded = false;
            bool queenAdded = false;
            bool navunAdded = false;
            RemnantWorldEvent ward13 = new RemnantWorldEvent();
            RemnantWorldEvent hideout = new RemnantWorldEvent();
            RemnantWorldEvent church = new RemnantWorldEvent();
            RemnantWorldEvent undying = new RemnantWorldEvent();
            RemnantWorldEvent queen = new RemnantWorldEvent();
            RemnantWorldEvent navun = new RemnantWorldEvent();
            RemnantWorldEvent ward17 = new RemnantWorldEvent();
            if (mode == ProcessMode.Campaign)
            {
                ward13.setKey("Ward13");
                ward13.Name = "Ward 13";
                ward13.Location = "Earth: Ward 13";
                ward13.Type = "Home";
                ward13.setMissingItems(this);
                if (ward13.MissingItems.Length > 0) orderedEvents.Add(ward13);

                hideout.setKey("FoundersHideout");
                hideout.Name = "Founder's Hideout";
                hideout.Location = "Earth: Fairview";
                hideout.Type = "Point of Interest";
                hideout.setMissingItems(this);
                if (hideout.MissingItems.Length > 0) orderedEvents.Add(hideout);

                church.setKey("RootMother");
                church.Name = "Root Mother";
                church.Location = "Earth: Church of the Harbinger";
                church.Type = "Siege";
                church.setMissingItems(this);

                undying.setKey("UndyingKing");
                undying.Name = "Undying King";
                undying.Location = "Rhom: Undying Throne";
                undying.Type = "World Boss";
                undying.setMissingItems(this);

                queen.Name = "Iskal Queen";
                queen.setKey("IskalQueen");
                queen.Location = "Corsus: The Mist Fen";
                queen.Type = "Point of Interest";
                queen.setMissingItems(this);

                navun.Name = "Fight With The Rebels";
                navun.setKey("SlaveRevolt");
                navun.Location = "Yaesha: Shrine of the Immortals";
                navun.Type = "Siege";
                navun.setMissingItems(this);

                ward17.setKey("Ward17");
                ward17.Name = "The Dreamer";
                ward17.Location = "Earth: Ward 17";
                ward17.Type = "World Boss";
                ward17.setMissingItems(this);
            }

            for(int i=0; i < zoneEvents["Earth"].Count; i++)
            {
                if (mode == ProcessMode.Campaign && !churchAdded && zoneEvents["Earth"][i].Location.Contains("Westcourt"))
                {
                    if (church.MissingItems.Length > 0) orderedEvents.Add(church);
                    churchAdded = true;
                }
                orderedEvents.Add(zoneEvents["Earth"][i]);
            }
            for (int i = 0; i < zoneEvents["Rhom"].Count; i++)
            {
                orderedEvents.Add(zoneEvents["Rhom"][i]);
            }
            if (mode == ProcessMode.Campaign && undying.MissingItems.Length > 0) orderedEvents.Add(undying);
            for (int i = 0; i < zoneEvents["Corsus"].Count; i++)
            {
                if (mode == ProcessMode.Campaign && !queenAdded && zoneEvents["Corsus"][i].Location.Contains("The Mist Fen"))
                {
                    if (queen.MissingItems.Length > 0) orderedEvents.Add(queen);
                    queenAdded = true;
                }
                orderedEvents.Add(zoneEvents["Corsus"][i]);
            }
            for (int i = 0; i < zoneEvents["Yaesha"].Count; i++)
            {
                if (mode == ProcessMode.Campaign && !navunAdded && zoneEvents["Yaesha"][i].Location.Contains("The Scalding Glade"))
                {
                    if (navun.MissingItems.Length > 0) orderedEvents.Add(navun);
                    navunAdded = true;
                }
                orderedEvents.Add(zoneEvents["Yaesha"][i]);
            }

            if (mode == ProcessMode.Campaign)
            {
                if (ward17.MissingItems.Length > 0) orderedEvents.Add(ward17);
            }

            for (int i=0; i < orderedEvents.Count; i++)
            {
                if (mode == ProcessMode.Campaign)
                {
                    this.CampaignEvents.Add(orderedEvents[i]);
                }
                else
                {
                    this.AdventureEvents.Add(orderedEvents[i]);
                }
            }
        }

        private string getZone(string textLine)
        {
            string zone = null;
            if (textLine.Contains("World_City"))
            {
                zone = "Earth";
            }
            else if (textLine.Contains("World_Wasteland"))
            {
                zone = "Rhom";
            }
            else if (textLine.Contains("World_Jungle"))
            {
                zone = "Yaesha";
            }
            else if (textLine.Contains("World_Swamp"))
            {
                zone = "Corsus";
            }
            return zone;
        }

        private string getEventType(string textLine)
        {
            string eventType = null;
            if (textLine.Contains("SmallD"))
            {
                eventType = "Side Dungeon";
            }
            else if (textLine.Contains("Quest_Boss"))
            {
                eventType = "World Boss";
            }
            else if (textLine.Contains("Siege"))
            {
                eventType = "Siege";
            }
            else if (textLine.Contains("Mini"))
            {
                eventType = "Miniboss";
            }
            else if (textLine.Contains("Quest_Event"))
            {
                if (textLine.Contains("Nexus"))
                {
                    eventType = "Siege";
                } else
                {
                    eventType = "Item Drop";
                }
            }
            else if (textLine.Contains("OverworldPOI"))
            {
                eventType = "Point of Interest";
            }
            return eventType;
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
                    inventories[i] = inventories[i].Substring(inventories[i].IndexOf("/Game/Characters/Player/Base/Character_Master_Player.Character_Master_Player_C"));
                    Regex rx = new Regex(@"/Items/Weapons(/[a-zA-Z0-9_]+)+/[a-zA-Z0-9_]+");
                    MatchCollection matches = rx.Matches(inventories[i]);
                    foreach (Match match in matches)
                    {
                        saveItems.Add(match.Value);
                    }

                    rx = new Regex(@"/Items/Armor/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+");
                    matches = rx.Matches(inventories[i]);
                    foreach (Match match in matches)
                    {
                        saveItems.Add(match.Value);
                    }

                    rx = new Regex(@"/Items/Trinkets/[a-zA-Z0-9_]+");
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

        public List<string> GetMissingItems()
        {
            return missingItems;
        }
    }
}
