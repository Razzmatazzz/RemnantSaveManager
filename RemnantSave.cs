using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RemnantSaveManager
{
    public class RemnantSave
    {
        private string profilePath;
        private List<RemnantCharacter> saveCharacters;

        public RemnantSave(string saveProfilePath)
        {
            if (!saveProfilePath.EndsWith("profile.sav"))
            {
                if (File.Exists(saveProfilePath+"\\profile.sav"))
                {
                    saveProfilePath += "\\profile.sav";
                } else
                {
                    throw new Exception(saveProfilePath + " is not a valid save.");
                }
            } else if (!File.Exists(saveProfilePath)) {
                throw new Exception(saveProfilePath + " does not exist.");
            }
            this.profilePath = saveProfilePath;
            saveCharacters = RemnantCharacter.GetCharactersFromSave(this.SaveFolderPath, RemnantCharacter.CharacterProcessingMode.NoEvents);
        }

        public string SaveFolderPath
        {
            get
            {
                return this.profilePath.Replace("\\profile.sav", "");
            }
        }

        public List<RemnantCharacter> Characters
        {
            get
            {
                return saveCharacters;
            }
        }

        public static Boolean ValidSaveFolder(String folder)
        {
            if (!File.Exists(folder + "\\profile.sav"))
            {
                return false;
            }
            return true;
        }

        public void UpdateCharacters()
        {
            saveCharacters = RemnantCharacter.GetCharactersFromSave(this.SaveFolderPath);
        }

        public void UpdateCharacters(RemnantCharacter.CharacterProcessingMode mode)
        {
            saveCharacters = RemnantCharacter.GetCharactersFromSave(this.SaveFolderPath, mode);
        }
    }
}
