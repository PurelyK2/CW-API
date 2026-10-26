using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using nickeltin.SDF.Runtime;
using UnityEngine;
using HarmonyLib;
using CWAPI.Items;

namespace CWAPI.Characters;

public class Blob : ModdedCharacter
{
    public Blob() : base("Blob", "Blob", "Blob", new BlobPin(), "Blob", Color.white, Color.grey, new Vector3(20f, 150f, 0f), new Vector3(-50f, 150f, 0f), new List<Type>(), Emotions.CactusFlip)
    {
    }
}