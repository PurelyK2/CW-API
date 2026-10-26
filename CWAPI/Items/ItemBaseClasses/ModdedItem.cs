using HarmonyLib;
using nickeltin;
using nickeltin.SDF;
using nickeltin.SDF.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace CWAPI.Items;

public abstract class ModdedItem : Item
{
    public static ValueTuple<bool, Sprite> ShouldSwapSDF = new(false, null);
    public abstract string SpritePath { get; }
    public virtual Sprite ItemSprite { get; private set; }
    public virtual SDFSpriteMetadataAsset ItemSDFSprite { get; private set; }

    public ModdedItem()
    {
        //Sprite Init
        ItemSprite = LoadSprite(SpritePath);
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
    Sprite LoadSprite(string path, float pixelsPerUnit = 200)
    {
        if (SpritePath == "") return Sprite.Create(new Texture2D(1,1), new Rect(), Vector2.zero);

        Assembly assembly = typeof(Plugin).Assembly;

        using (Stream stream = assembly.GetManifestResourceStream(path))
        {
            if (stream == null)
            {
                Debug.LogError($"[CWAPI] Failed to find embedded resource: {path}");
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
}