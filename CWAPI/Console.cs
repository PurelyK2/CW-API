using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CWAPI;

public class Console
{
    private static ConsoleController console;
    public static MethodInfo onOrOffMethod;
    public static MethodInfo tryExecuteCommandMethod;
    public static FieldInfo inputFieldField;
    public static Console Instance;

    public void UpdateConsole()
    {
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.BackQuote))
        {
            ToggleBuiltInConsole();
        }

        if (console != null && console.IsOn && UnityEngine.Input.GetKeyUp(UnityEngine.KeyCode.Return))
        {
            ExecuteBuiltInConsoleInput();
        }
    }
    private static void ToggleBuiltInConsole()
    {
        if (console == null)
        {
            console = UnityEngine.Object.FindFirstObjectByType<ConsoleController>();
        }

        if (console == null)
        {
            Log.LogWarning("ConsoleController not found.");
            return;
        }

        if (onOrOffMethod == null)
        {
            onOrOffMethod = typeof(ConsoleController).GetMethod(
                "OnOrOff",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
        }

        if (onOrOffMethod == null)
        {
            Log.LogWarning("ConsoleController.OnOrOff method not found.");
            return;
        }

        console.IsOn = !console.IsOn;
        onOrOffMethod.Invoke(console, null);

        Log.LogInfo("Console toggled: " + console.IsOn);
    }
    private static void ExecuteBuiltInConsoleInput()
    {
        if (console == null)
            return;

        if (inputFieldField == null)
        {
            inputFieldField = typeof(ConsoleController).GetField(
                "_inputField",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
        }

        if (tryExecuteCommandMethod == null)
        {
            tryExecuteCommandMethod = typeof(ConsoleController).GetMethod(
                "TryExecuteCommand",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
        }

        if (inputFieldField == null || tryExecuteCommandMethod == null)
        {
            Log.LogWarning("Console input field or TryExecuteCommand method not found.");
            return;
        }

        TMP_InputField input = inputFieldField.GetValue(console) as TMP_InputField;

        if (input == null)
        {
            Log.LogWarning("Console TMP_InputField not found.");
            return;
        }

        tryExecuteCommandMethod.Invoke(console, new object[] { input.text });
    }
}