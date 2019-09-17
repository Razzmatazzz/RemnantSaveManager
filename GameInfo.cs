using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Net;
using System.IO;

namespace RemnantSaveManager
{
    class GameInfo
    {
        private static List<string> zones = new List<string>();
        private static Dictionary<string, string[]> eventItem = new Dictionary<string, string[]>();
        private static Dictionary<string, string> subLocations = new Dictionary<string, string>();
        private static Dictionary<string, string> mainLocations = new Dictionary<string, string>();
        public static Dictionary<string, string[]> EventItem
        {
            get
            {
                if (eventItem.Count == 0)
                {
                    RefreshGameInfo();
                }

                return eventItem;
            }
        }
        public static List<string> Zones
        {
            get
            {
                if (zones.Count == 0)
                {
                    RefreshGameInfo();
                }

                return zones;
            }
        }
        public static Dictionary<string, string> SubLocations
        {
            get {
                if (subLocations.Count == 0)
                {
                    RefreshGameInfo();
                }

                return subLocations;
            }
        }
        public static Dictionary<string, string> MainLocations
        {
            get
            {
                if (mainLocations.Count == 0)
                {
                    RefreshGameInfo();
                }

                return mainLocations;
            }
        }

        public static void RefreshGameInfo()
        {
            zones.Clear();
            eventItem.Clear();
            subLocations.Clear();
            mainLocations.Clear();
            string eventName = null;
            List<string> eventItems = new List<string>();
            XmlTextReader reader = new XmlTextReader("GameInfo.xml");
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
                        } else if (reader.Name.Equals("Zone"))
                        {
                            zones.Add(reader.GetAttribute("name"));
                        }
                        else if (reader.Name.Equals("SubLocation"))
                        {
                            subLocations.Add(reader.GetAttribute("eventName"), reader.GetAttribute("location"));
                        }
                        else if (reader.Name.Equals("MainLocation"))
                        {
                            mainLocations.Add(reader.GetAttribute("key"), reader.GetAttribute("name"));
                        }
                        break;
                    case XmlNodeType.Text:
                        if (eventName != null)
                        {
                            eventItems.Add(reader.Value);
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name.Equals("Event"))
                        {
                            eventItem.Add(eventName, eventItems.ToArray());
                            eventName = null;
                            eventItems.Clear();
                        }
                        break;
                }
            }
            reader.Close();
        }

        public static string CheckForNewGameInfo()
        {
            string message = "No new game info found.";
            try
            {
                WebClient client = new WebClient();
                client.DownloadFile("https://raw.githubusercontent.com/Razzmatazzz/RemnantSaveManager/master/Resources/GameInfo.xml", "TempGameInfo.xml");
                
                XmlTextReader reader = new XmlTextReader("TempGameInfo.xml");
                reader.WhitespaceHandling = WhitespaceHandling.None;
                int remoteversion = 0;
                int localversion = 0;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name.Equals("GameInfo"))
                        {
                            remoteversion = int.Parse(reader.GetAttribute("version"));
                            break;
                        }
                    }
                }
                reader.Close();
                if (File.Exists("GameInfo.xml"))
                {
                    reader = new XmlTextReader("GameInfo.xml");
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name.Equals("GameInfo"))
                            {
                                localversion = int.Parse(reader.GetAttribute("version"));
                                break;
                            }
                        }
                    }
                    reader.Close();

                    if (remoteversion > localversion)
                    {
                        File.Delete("GameInfo.xml");
                        File.Move("TempGameInfo.xml", "GameInfo.xml");
                        RefreshGameInfo();
                        message = "Game info updated!";
                    }
                    else
                    {
                        File.Delete("TempGameInfo.xml");
                    }
                } else
                {
                    File.Move("TempGameInfo.xml", "GameInfo.xml");
                    RefreshGameInfo();
                    message = "Game info updated!";
                }
            } catch (Exception ex)
            {
                message = "Error checking for new game info: " + ex.Message;
            }

            return message;
        }
    }
}
