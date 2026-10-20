using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MorePlatforms
{
    // Native Unity UI, parented to the existing creation menu and using its artwork.
    public sealed class CampaignSettingsRow : MonoBehaviour
    {
        internal int Value;
        private Text number;
        private Button minus;
        private Button plus;
        private GameObject row;
        private const float RowHeight = 44f;
        private static readonly System.Reflection.FieldInfo Main =
            AccessTools.Field(typeof(Menu_NewGameSettings), "mS_");

        internal void Build(Menu_NewGameSettings menu)
        {
            if (row) return;
            var window = menu.transform.Find("WindowMain") as RectTransform;
            var source = menu.uiObjects[18].transform.parent;
            var labelSource = menu.uiObjects[3].GetComponentInChildren<Text>(true);
            if (!window || !source || !labelSource)
                throw new InvalidOperationException("Layout do menu de nova campanha nao reconhecido.");

            // Insert below the title, moving existing contents down as a unit.
            // Stretch backgrounds/title bars keep their anchors and original placement.
            foreach (Transform child in window)
            {
                if (child.name == "Fill_Window" || child.name == "Fill_Titleleiste" ||
                    child.name == "Titelleiste") continue;
                var rect = child as RectTransform;
                if (rect) rect.anchoredPosition -= new Vector2(0, RowHeight *
                    (rect.anchorMin.y + rect.anchorMax.y) * 0.5f);
            }
            window.sizeDelta += new Vector2(0, RowHeight);
            row = new GameObject("MorePlatformsCampaignSetting", typeof(RectTransform));
            row.transform.SetParent(window, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0, 1);
            rowRect.pivot = new Vector2(0, 0.5f);
            rowRect.anchoredPosition = new Vector2(59, -94);
            rowRect.sizeDelta = new Vector2(503, 32);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(row.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = new Vector2(-120, 0);
            var label = labelObject.GetComponent<Text>();
            label.font = labelSource.font;
            label.fontSize = 16;
            label.fontStyle = labelSource.fontStyle;
            label.color = labelSource.color;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            label.text = "Máximo de plataformas por jogo";

            var counter = UnityEngine.Object.Instantiate(source.gameObject, row.transform, false);
            counter.name = "PlatformLimitCounter";
            var counterRect = counter.GetComponent<RectTransform>();
            counterRect.anchorMin = counterRect.anchorMax = new Vector2(0, 0.5f);
            counterRect.anchoredPosition = new Vector2(436, 0);
            number = counter.GetComponentInChildren<Text>(true);
            var buttons = counter.GetComponentsInChildren<Button>(true);
            if (buttons.Length != 2 || !number)
                throw new InvalidOperationException("Controle numerico do menu nao reconhecido.");
            minus = buttons[0];
            plus = buttons[1];
            if (minus.GetComponent<RectTransform>().anchoredPosition.x >
                plus.GetComponent<RectTransform>().anchoredPosition.x)
            {
                var swap = minus; minus = plus; plus = swap;
            }
            // Replace serialized callbacks too; RemoveAllListeners alone would retain them.
            minus.onClick = new Button.ButtonClickedEvent();
            plus.onClick = new Button.ButtonClickedEvent();
            minus.onClick.AddListener(delegate { Change(-1); });
            plus.onClick.AddListener(delegate { Change(1); });
            ResetValue(menu);
        }

        internal void ResetValue(Menu_NewGameSettings menu)
        {
            Value = Plugin.Maximum.Value;
            var main = Main.GetValue(menu) as mainScript;
            row.SetActive(!main || !main.multiplayer);
            Refresh();
        }

        private void Change(int delta)
        {
            Value = Math.Max(4, Math.Min(16, Value + delta));
            Refresh();
        }

        private void Refresh()
        {
            number.text = Value.ToString();
            minus.interactable = Value > 4;
            plus.interactable = Value < 16;
        }
    }

    [HarmonyPatch(typeof(Menu_NewGameSettings), "OnEnable")]
    internal static class NewCampaignMenu
    {
        static void Postfix(Menu_NewGameSettings __instance)
        {
            var row = __instance.GetComponent<CampaignSettingsRow>();
            if (!row)
            {
                row = __instance.gameObject.AddComponent<CampaignSettingsRow>();
                row.Build(__instance);
            }
            else row.ResetValue(__instance);
        }
    }

    [HarmonyPatch(typeof(Menu_NewGameSettings), "BUTTON_OK")]
    internal static class StartCampaign
    {
        static void Prefix(Menu_NewGameSettings __instance, out int __state)
        {
            __state = Plugin.CampaignMaximum;
            var row = __instance.GetComponent<CampaignSettingsRow>();
            Plugin.SetCampaignMaximum(row ? row.Value : Plugin.Maximum.Value);
        }

        static Exception Finalizer(Exception __exception, int __state)
        {
            if (__exception != null) Plugin.SetCampaignMaximum(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(savegameScript), "SaveGlobals")]
    internal static class SaveCampaignLimit
    {
        internal const string Key = "br_sapo_moreplatforms_campaign_limit_v1";
        static void Postfix(ES3Writer writer)
        {
            writer.Write<int>(Key, Plugin.CampaignMaximum);
        }
    }

    [HarmonyPatch(typeof(savegameScript), "LoadGlobals")]
    internal static class LoadCampaignLimit
    {
        static void Postfix(ES3Reader reader)
        {
            Plugin.SetCampaignMaximum(reader.Read<int>(SaveCampaignLimit.Key, Plugin.Maximum.Value));
        }
    }
}
