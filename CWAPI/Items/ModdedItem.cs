using CWAPI.Utilities;
using HarmonyLib;
using nickeltin;
using nickeltin.SDF;
using nickeltin.SDF.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using static CWAPI.Tiles.ModdedTileType;

namespace CWAPI.Items;

public abstract class ModdedItem : Item
{
    public static ValueTuple<bool, Sprite> ShouldSwapSDF = new(false, null);
    public abstract string SpritePath { get; }
    public virtual Sprite ItemSprite { get; private set; }
    public virtual SDFSpriteMetadataAsset ItemSDFSprite { get; private set; }
    public virtual bool IsUnlocked => true;

    public ModdedItem()
    {
        //Sprite Init
        ItemSprite = LoadSprite();
        ItemSprite.name = "ModdedItem";

        //SDF Init
        ShouldSwapSDF = new(true, ItemSprite);
        ItemSDFSprite = ScriptableObject.CreateInstance<SDFSpriteMetadataAsset>();
        ShouldSwapSDF = new(false, null);

        ItemSprite.AddScriptableObject(ItemSDFSprite);

        ItemSpriteData spriteData = new ItemSpriteData(ItemSpriteUsage.Default, "Ambulance");
        spriteData.ItemSprite = ItemSprite;

        SpriteData.Add(spriteData);
    }

    public override Sprite GetCurrentSprite()
    {
        return ItemSprite;
    }
    Sprite LoadSprite(float pixelsPerUnit = 200)
    {
        if (SpritePath == "") return Sprite.Create(new Texture2D(1,1), new Rect(), Vector2.zero);

        Assembly assembly = this.GetType().Assembly;

        using (Stream stream = assembly.GetManifestResourceStream(SpritePath))
        {
            if (stream == null)
            {
                Debug.LogError($"[CWAPI] Failed to find embedded resource: {SpritePath}");
                return null;
            }

            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (ImageConversion.LoadImage(texture, buffer))
            {
                Rect rect = new Rect(0, 0, texture.width, texture.height);
                Vector2 pivot = new Vector2(0.5f, 0.5f);

                return Sprite.Create(texture, rect, pivot, pixelsPerUnit);
            }
        }

        return null;
    }

    public override SDFSpriteMetadataAsset GetCurrentSDFSprite()
    {
        return ItemSDFSprite;
    }

    [HarmonyPatch(typeof(SDFSpriteMetadataAsset), MethodType.Constructor)]
    public static class ReplaceSDFMetadataSprite
    {
        public static void Postfix(ref SDFSpriteMetadata ____metadata)
        {
            if (!ShouldSwapSDF.Item1) return;

            Log.LogInfo("Replacing SDF Metadata");

            SDFSpriteMetadata newMetadata = new SDFSpriteMetadata();
            newMetadata.SourceSprite = ShouldSwapSDF.Item2;

            ____metadata = newMetadata;
        }
    }

    [HarmonyPatch(typeof(GameStatics), "IsUnlockRequired")]
    public static class OverrideUnlockStuff
    {
        public static void Postfix(Type itemType, ref bool __result)
        {
            if(itemType.IsSubclassOf(typeof(ModdedItem)))
            {
                ModdedItem item = ItemUtils.ModdedItems.FirstOrDefault(item => item.GetType() == itemType);
                __result = item.IsUnlocked;
            }
        }
    }

    //Fix Populating Collection Cards For Pins
    [HarmonyPatch(typeof(CollectionCard), "Populate")]
    public static class LockedCardPopulatePatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // 1. Target the .Find method call on the generic list
            // This is the exact moment the game finishes its LINQ search and returns the Character
            MethodInfo listFindMethod = AccessTools.Method(typeof(List<Character>), nameof(List<Character>.Find));
            MethodInfo hookMethod = AccessTools.Method(typeof(LockedCardPopulatePatch), nameof(LockedCardPopulatePatch.GetCharacter));

            var matcher = new CodeMatcher(instructions);

            // 2. Find the moment the game finds its vanilla character
            matcher.MatchStartForward(new CodeMatch(OpCodes.Callvirt, listFindMethod));

            if (matcher.IsValid)
            {
                // 3. Move exactly 1 instruction past 'Callvirt Find' so the Character is on top of the stack
                matcher.Advance(1);

                // 4. Inject our hook. At this point, the stack contains [Character]
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_1),          // Load 'Item item' parameter onto the stack
                    new CodeInstruction(OpCodes.Call, hookMethod)  // Calls GetCharacter(originalCharacter, item)
                );
            }
            else
            {
                Log.LogInfo("Couldn't Find Patch Spot For Character Interception via List.Find");
            }

            return matcher.InstructionEnumeration();
        }

        // Keep this signature exactly as is!
        static Character GetCharacter(Character original, Item item)
        {
            Log.LogInfo("Getting Character From Full Characters List Instead");

            Character character = CharacterUtils.FullCharactersList.FirstOrDefault(c => c.GetCharacterItem()?.GetType() == item?.GetType());

            if (character != null)
            {
                Log.LogInfo("Pin Character: " + character.GetName());
                return character;
            }

            Log.LogError("Error: Character is null");
            return original; // Safely fall back to the vanilla character if our lookup fails
        }
    }
}