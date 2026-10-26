using CWAPI.Items;
using CWAPI.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

namespace CWAPI.Characters;

public abstract class ModdedCharacter : Character
{
    public virtual Sprite CharacterSprite => null;
    public virtual RuntimeAnimatorController CharacterAnimator => null;
    public virtual bool StartUnlocked => true;
    public ModdedCharacter(string name, string steamAchievementName, string description, Item characterItem, string artFileName, Color uiColorA, Color uiColorB, Vector3 endScreenVictoryOffset, Vector3 endScreenDefeatOffset, List<Type> goodIntroItemTypes, Emotions myEmotion) : base(name, steamAchievementName, description, characterItem, artFileName, uiColorA, uiColorB, endScreenVictoryOffset, endScreenDefeatOffset, goodIntroItemTypes, myEmotion)
    {
    }

    public override Sprite GetCharacterSprite()
    {
        if(CharacterSprite == null)
        {
            return Resources.Load<Sprite>("Piero/Players/Crystal/Crystal");
        }

        return CharacterSprite;
    }
    public override RuntimeAnimatorController GetRuntimeAnimatorController()
    {
        if(CharacterAnimator == null)
        {
            return Resources.Load<RuntimeAnimatorController>("Piero/Players/Crystal/Crystal");
        }

        return CharacterAnimator;
    }

    // Override Save Slot Controller Save Slot Stuff To Properly Show
    [HarmonyPatch(typeof(SaveSlotController), "Populate", new System.Type[] {typeof(SaveFile), typeof(bool)})]
    public static class ReplaceMiniCharacterImages
    {
        public static void Postfix(SaveFile saveFile, bool isNewFile, ref CharacterSelectTile[] ____characterTiles)
        {
            if (isNewFile) return;

            foreach (var tile in ____characterTiles)
            {
                tile.gameObject.SetActive(false);
            }

            ____characterTiles = CreateCharacterTiles(____characterTiles, ____characterTiles[0].transform.parent.gameObject).ToArray();

            foreach (var tile in ____characterTiles)
            {
                tile.gameObject.SetActive(true);
            }
        }

        static List<CharacterSelectTile> CreateCharacterTiles(CharacterSelectTile[] vanillaTiles, GameObject tileParent)
        {
            List<CharacterSelectTile> allTiles = new List<CharacterSelectTile>(vanillaTiles);
            GameObject tilePrefab = allTiles[0].gameObject;

            foreach (ModdedCharacter moddedCharacter in CharacterUtils.ModdedCharacters)
            {
                GameObject newTile = GameObject.Instantiate(tilePrefab, tileParent.transform, false);

                CharacterSelectTile charSelectTile = newTile.GetComponentInChildren<CharacterSelectTile>();
                charSelectTile.Populate(moddedCharacter, true, true);

                allTiles.Add(charSelectTile);
            }

            //make every tile
            //scale tiles to fit in the same size grid no matter what
            int rowCount = (int)Mathf.Ceil((float)allTiles.Count / 4f);

            GridLayoutGroup gridLayout = tileParent.GetComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
            gridLayout.cellSize *= 3f / (float)rowCount;

            return allTiles;
        }
    }

    // Override Select Screen For Modded Characters
    [HarmonyPatch(typeof(CharacterSelectController), "GetCharacters")]
    public static class AddCharactersToSelectScreen
    {
        [HarmonyPriority(Priority.First)]
        public static void Postfix(ref Dictionary<Type, bool> ____characters, ref CharacterSelectController __instance)
        {
            foreach(ModdedCharacter character in CharacterUtils.ModdedCharacters)
            {
                ____characters.Add(character.GetType(), SaveManager.GetUnlockedCharacters().Contains(character.GetType()));
                if (__instance.IsInChallenge())
                {
                    if ((character as Character).IsSecret())
                    {
                        ____characters.Remove(character.GetType());
                    }
                }
            }
        }
    }

    // Override Unlock Checks
    [HarmonyPatch(typeof(SaveManager), "GetUnlockedCharacters")]
    public static class GetUnlockedModdedCharacters
    {
        public static void Postfix(ref List<Type> __result)
        {
            foreach (ModdedCharacter character in CharacterUtils.ModdedCharacters)
            {
                if (character.StartUnlocked) __result.Add(character.GetType());
            }
        }
    }

    [HarmonyPatch(typeof(SaveManager), "IsCharacterUnlocked", new System.Type[] { typeof(Character) })]
    public static class GetUnlockedModdedCharacters2
    {
        public static void Postfix(ref bool __result, Character character)
        {
            if (__result) return;

            foreach (ModdedCharacter moddedCharacter in CharacterUtils.ModdedCharacters)
            {
                if (moddedCharacter.GetType() == character.GetType())
                {
                    __result = SaveManager.GetUnlockedCharacters().Contains(character.GetType());
                    return;
                }
            }

            Log.LogError("Didn't Find Character In Modded Characters List");
            return;
        }
    }

    [HarmonyPatch(typeof(SaveManager), "GetHighestCompletedAscension", new System.Type[] { typeof(Type) })]
    public static class GetHighestAscension
    {
        public static void Prefix(ref SaveFile ____saveFile)
        {
            foreach(ModdedCharacter character in CharacterUtils.ModdedCharacters)
            {
                if (____saveFile.CharacterHighestCompletedAscensions.ContainsKey(character.GetType())) continue;

                ____saveFile.CharacterHighestCompletedAscensions.Add(character.GetType(), -1);
            }
        }
    }
}
