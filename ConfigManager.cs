using BepInEx;
using DebugUtils;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConfigManager
{
    [BepInPlugin("com.github.johndowson.ConfigManager", "ConfigManager", "1.0.0.0")]
    public class ConfigManager : BaseUnityPlugin
    {
        private static readonly Harmony harmony = new(typeof(ConfigManager).GetCustomAttributes(typeof(BepInPlugin), false)
            .Cast<BepInPlugin>()
            .First()
            .GUID);

        private static List<ButtonDef> Buttons = new List<ButtonDef>();

        public static void RegisterButton(string name, string screenName, KeyCode code)
        {
            var button = new ButtonDef(name, screenName, code);
            Buttons.Add(button);
        }

        private class ButtonDef
        {
            public string name;
            public string displayName;
            public KeyCode code;

            public ButtonDef(string name, string displayName, KeyCode code)
            {
                this.name = name;
                this.displayName = displayName;
                this.code = code;
            }
            public ButtonDef(string name, string GUID, string displayName, KeyCode code)
            {
                this.name = GUID + name;
                this.displayName = displayName;
                this.code = code;
            }
        }

#pragma warning disable IDE0051 // Remove unused private members
        private void Awake()
        {
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        [HarmonyPatch(typeof(Settings), "SetupKeys")]
        static class Settings_Patch
        {
            static public void Prefix(Settings __instance)
            {
                var keyTransformBase = __instance.m_keys[__instance.m_keys.Count() - 1].m_keyTransform;
                var panelTransform = keyTransformBase.parent.parent.parent.GetComponent<RectTransform>();
                var tabHandler = keyTransformBase.parent.parent.parent.GetComponentInChildren<TabHandler>();
                var tabTransform = tabHandler.GetComponentInParent<RectTransform>();
                float verticalOffset = -20.0f;
                float evenHorizontalOffset = -232.0f - keyTransformBase.localPosition.x;
                foreach ((var button, bool even) in Buttons.Select((value, i) => (value, i % 2 == 0)))
                {
                    var keyTransform = Instantiate(keyTransformBase, keyTransformBase.parent);

                    if (even)
                    {
                        keyTransform.localPosition = keyTransformBase.localPosition +
                            new Vector3(evenHorizontalOffset, verticalOffset, 0.0f);
                        panelTransform.anchorMin += new Vector2(0.0f, -0.02f);
                        tabTransform.anchoredPosition += new Vector2(0.0f, 10.0f);
                    }
                    else
                    {
                        keyTransform.localPosition = keyTransformBase.localPosition +
                            new Vector3(0.0f, verticalOffset, 0.0f);

                        verticalOffset -= 20.0f;
                    }

                    keyTransform.GetComponentInChildren<UnityEngine.UI.Text>().text = button.displayName + ':';

                    var keySetting = new Settings.KeySetting
                    {
                        m_keyName = button.name,
                        m_keyTransform = keyTransform
                    };
                    __instance.m_keys.Add(keySetting);
                }
                if (Buttons.Count() % 2 != 0)
                    panelTransform.anchorMin += new Vector2(0.0f, -0.02f);
                tabTransform.anchoredPosition += new Vector2(0.0f, 12.0f);
            }
        }

        [HarmonyPatch(typeof(ZInput), "Reset")]
        static class ZInput_Patch
        {
            static public void Postfix(ZInput __instance)
            {
                foreach (var button in Buttons)
                {
                    __instance.AddButton(button.name, button.code);
                }
            }
        }
    }
}
