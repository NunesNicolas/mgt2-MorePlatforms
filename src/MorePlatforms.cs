using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MorePlatforms
{
    [BepInPlugin(Id, "Mais plataformas", "1.2.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "br.sapo.mgt2.moreplatforms";
        internal static Plugin Instance;
        internal static ConfigEntry<int> Maximum;
        internal static int CampaignMaximum;
        internal static Menu_DevGame Dev;
        internal static int CreationCapacity = 4;
        internal static readonly FieldInfo MainField = AccessTools.Field(typeof(Menu_DevGame), "mS_");
        private Harmony harmony;
        private bool visible;
        private Rect window = new Rect(24, 90, 450, 470);
        private int draft;
        private string message = "";
        private bool ready;
        private GameObject blocker;
        private RectTransform blockerRect;

        private void Awake()
        {
            Instance = this;
            Maximum = Config.Bind("Desenvolvimento", "MaximoDePlataformas", 8,
                new ConfigDescription("Limite para novos projetos. F8 abre as configuracoes no jogo.",
                    new AcceptableValueRange<int>(4, 16)));
            SetCampaignMaximum(Maximum.Value);
            harmony = new Harmony(Id);
            try
            {
                harmony.PatchAll(typeof(Plugin).Assembly);
                ready = true;
                Logger.LogInfo("Mais plataformas carregado. F8: configuracoes; limite " + Maximum.Value);
            }
            catch (Exception ex)
            {
                harmony.UnpatchSelf();
                Logger.LogError("Mod desativado: nao foi possivel aplicar todos os patches. " + ex);
            }
        }

        private void OnDestroy()
        {
            if (harmony != null) harmony.UnpatchSelf();
            if (blocker) Destroy(blocker);
        }

        private void Update()
        {
            if (ready && Input.GetKeyDown(KeyCode.F8)) visible = !visible;
            if (ready) UpdateBlocker();
        }

        private void UpdateBlocker()
        {
            if (!blocker && visible)
            {
                blocker = new GameObject("MorePlatformsInputBlocker", typeof(RectTransform),
                    typeof(Canvas), typeof(GraphicRaycaster));
                DontDestroyOnLoad(blocker);
                var canvas = blocker.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32760;
                var panel = new GameObject("WindowHitArea", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(blocker.transform, false);
                panel.GetComponent<Image>().color = new Color(0, 0, 0, 0);
                blockerRect = panel.GetComponent<RectTransform>();
                blockerRect.anchorMin = blockerRect.anchorMax = new Vector2(0, 1);
                blockerRect.pivot = new Vector2(0, 1);
            }
            if (!blocker) return;
            blocker.SetActive(visible);
            blockerRect.anchoredPosition = new Vector2(window.x, -window.y);
            blockerRect.sizeDelta = new Vector2(window.width, window.height);
        }

        internal static bool Offline(Menu_DevGame menu)
        {
            var main = menu ? MainField.GetValue(menu) as mainScript : null;
            return main && !main.multiplayer;
        }

        internal static void SetCampaignMaximum(int value)
        {
            CampaignMaximum = Math.Max(4, Math.Min(16, value));
            if (Instance) Instance.draft = CampaignMaximum;
        }

        internal static bool Allowed(Menu_DevGame menu)
        {
            if (!menu || !menu.gameObject.activeInHierarchy || !Offline(menu) || menu.typ_contractGame)
                return false;
            int mode = menu.uiObjects[146].GetComponent<Dropdown>().value;
            return mode != 1 && mode != 4;
        }

        internal static void ResizeIds(ref int[] ids, int size)
        {
            int old = ids == null ? 0 : ids.Length;
            if (old == size) return;
            Array.Resize(ref ids, size);
            for (int i = old; i < size; i++) ids[i] = -1;
        }

        internal static void EnsureGame(gameScript game, int size)
        {
            size = Math.Max(4, Math.Max(size, game.gamePlatform == null ? 0 : game.gamePlatform.Length));
            ResizeIds(ref game.gamePlatform, size);
            if (game.gamePlatformScript == null || game.gamePlatformScript.Length != size)
                Array.Resize(ref game.gamePlatformScript, size);
        }

        internal static void Refresh(Menu_DevGame menu)
        {
            // Reuse the original summary/cost refresh through an existing UI slot.
            menu.SetPlatform(0, menu.g_GamePlatform[0]);
        }

        internal static void SetExtra(Menu_DevGame menu, int slot, int id)
        {
            if (slot < 4 || slot >= menu.g_GamePlatform.Length) return;
            if (id >= 0 && (!Allowed(menu) || Array.IndexOf(menu.g_GamePlatform, id) >= 0)) return;
            menu.g_GamePlatform[slot] = id;
            Refresh(menu);
        }

        private void OnGUI()
        {
            if (!ready) return;
            if (!visible) return;
            window.width = Math.Min(450, Screen.width - 16);
            window.height = Math.Min(530, Screen.height - 32);
            window.x = Mathf.Clamp(window.x, 0, Math.Max(0, Screen.width - window.width));
            window.y = Mathf.Clamp(window.y, 0, Math.Max(0, Screen.height - window.height));
            window = GUILayout.Window(190824, window, DrawWindow, "Mais plataformas - configuracoes");
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Maximo por jogo: " + draft);
            if (GUILayout.Button("-", GUILayout.Width(32))) draft = Math.Max(4, draft - 1);
            if (GUILayout.Button("+", GUILayout.Width(32))) draft = Math.Min(16, draft + 1);
            if (GUILayout.Button("Fechar", GUILayout.Width(65))) visible = false;
            GUILayout.EndHorizontal();
            draft = Mathf.RoundToInt(GUILayout.HorizontalSlider(draft, 4, 16));
            if (GUILayout.Button("Salvar configuracao")) ApplyMaximum();
            GUILayout.Label("4 a 16 plataformas. Jogos ja criados mantem suas plataformas.");
            if (message.Length != 0) GUILayout.Label(message);
            GUILayout.Label("Selecione as plataformas na tela de desenvolvimento. Role a lista para ver as extras.");
            GUI.DragWindow(new Rect(0, 0, window.width, 20));
        }

        private void ApplyMaximum()
        {
            if (Dev && Dev.gameObject.activeInHierarchy && Dev.g_GamePlatform != null)
            {
                for (int i = draft; i < Dev.g_GamePlatform.Length; i++)
                    if (Dev.g_GamePlatform[i] >= 0)
                    {
                        message = "Remova as plataformas acima do novo limite antes de reduzir.";
                        return;
                    }
                if (Offline(Dev)) ResizeIds(ref Dev.g_GamePlatform, draft);
            }
            Maximum.Value = draft;
            SetCampaignMaximum(draft);
            Config.Save();
            message = "Configuracao salva: " + draft + " plataformas.";
            if (Dev && Dev.gameObject.activeInHierarchy && Offline(Dev)) Refresh(Dev);
        }

        internal static void Error(string text, Exception ex)
        {
            Instance.Logger.LogError(text + ": " + ex);
        }
    }

    [HarmonyPatch(typeof(Menu_DevGame), "OnEnable")]
    internal static class DevOpen
    {
        static void Postfix(Menu_DevGame __instance)
        {
            Plugin.Dev = __instance;
            if (Plugin.Offline(__instance))
                Plugin.ResizeIds(ref __instance.g_GamePlatform, Plugin.CampaignMaximum);
            var view = __instance.GetComponent<PlatformScroll>();
            if (!view) view = __instance.gameObject.AddComponent<PlatformScroll>();
            view.Build(__instance);
            view.Sync(true);
        }
    }

    [HarmonyPatch(typeof(Menu_DevGame), "SetPlatform")]
    internal static class ExtraSelection
    {
        static bool Prefix(Menu_DevGame __instance, int slot, int platform_)
        {
            if (slot >= 4)
            {
                Plugin.SetExtra(__instance, slot, platform_);
                return false;
            }
            if (platform_ >= 0)
            {
                int existing = Array.IndexOf(__instance.g_GamePlatform, platform_);
                if (existing >= 0 && existing != slot) return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Menu_DevGame), "DROPDOWN_PlattformTyp")]
    internal static class ModeChanged
    {
        static void Prefix(Menu_DevGame __instance)
        {
            // Clear before vanilla calls SetPlatform and recalculates totals.
            if (!Plugin.Allowed(__instance) && __instance.g_GamePlatform != null)
                for (int i = 4; i < __instance.g_GamePlatform.Length; i++)
                    __instance.g_GamePlatform[i] = -1;
        }
    }

    [HarmonyPatch(typeof(Menu_DevGame_Platform), "Init")]
    internal static class PickerTitle
    {
        static void Postfix(Menu_DevGame_Platform __instance, int nr)
        {
            if (nr >= 4) __instance.uiObjects[4].GetComponent<Text>().text = "Plataforma " + (nr + 1);
        }
    }

    [HarmonyPatch(typeof(Item_DevGame_Platform), "SetData")]
    internal static class DuplicateSelection
    {
        static void Postfix(Item_DevGame_Platform __instance)
        {
            int[] ids = __instance.devGame_ ? __instance.devGame_.g_GamePlatform :
                (__instance.changePlatform_ ? __instance.changePlatform_.g_GamePlatform : null);
            if (ids != null && Array.IndexOf(ids, __instance.myID) >= 4)
            {
                __instance.GetComponent<Button>().interactable = false;
                __instance.uiObjects[3].SetActive(true);
            }
        }
    }

    // Scope capacity to player creation. NPC games keep the original arrays.
    [HarmonyPatch]
    internal static class CreationScope
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Menu_DevGame), "BUTTON_Start");
            yield return AccessTools.Method(typeof(Menu_Dev_AddonDo), "BUTTON_Start");
            yield return AccessTools.Method(typeof(Menu_Dev_MMOAddon), "BUTTON_Start");
        }
        static void Prefix(object __instance, out int __state)
        {
            __state = Plugin.CreationCapacity;
            var dev = __instance as Menu_DevGame;
            var addon = __instance as Menu_Dev_AddonDo;
            var mmo = __instance as Menu_Dev_MMOAddon;
            if (dev && (dev.typ_contractGame || !Plugin.Offline(dev)))
                Plugin.ResizeIds(ref dev.g_GamePlatform, 4);
            int[] ids = dev ? dev.g_GamePlatform : addon ? addon.gS_.gamePlatform : mmo.gS_.gamePlatform;
            Plugin.CreationCapacity = ids.Length;
        }
        static Exception Finalizer(Exception __exception, int __state)
        {
            Plugin.CreationCapacity = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(games), "CreateNewGame")]
    internal static class NewGameCapacity
    {
        static void Postfix(gameScript __result)
        {
            if (__result && Plugin.CreationCapacity > 4)
                Plugin.EnsureGame(__result, Plugin.CreationCapacity);
        }
    }

    [HarmonyPatch(typeof(gameScript), "FindMyPlatforms")]
    internal static class PlatformCache
    {
        static void Prefix(gameScript __instance)
        {
            Plugin.EnsureGame(__instance, 4);
            Array.Clear(__instance.gamePlatformScript, 0, __instance.gamePlatformScript.Length);
        }
    }

    [HarmonyPatch(typeof(Menu_Dev_ChangePlatform), "Init")]
    internal static class ChangeMenuCapacity
    {
        static void Prefix(Menu_Dev_ChangePlatform __instance, gameScript game_)
        {
            Plugin.ResizeIds(ref __instance.g_GamePlatform, game_.gamePlatform.Length);
        }
        static void Postfix(Menu_Dev_ChangePlatform __instance)
        {
            __instance.SetPlatform(0, __instance.g_GamePlatform[0], true);
        }
    }

    [HarmonyPatch(typeof(Menu_Dev_ChangePlatform), "SetPlatform")]
    internal static class ChangeExtra
    {
        static bool Prefix(Menu_Dev_ChangePlatform __instance, int slot, int platform_)
        {
            if (slot < 4) return true;
            // Preserve extra slots while the original replacement UI edits the first four.
            __instance.g_GamePlatform[slot] = platform_;
            return false;
        }
    }

    // These specific views have four serialized icon widgets. Only their display
    // loops are bounded, never the simulation, costs, revenue, or save loops.
    [HarmonyPatch]
    internal static class FourIconViews
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in new[] {
                typeof(Menu_Dev_GameEntwicklungsbericht), typeof(Menu_Dev_MarketAnalyse),
                typeof(Menu_Dev_ShowConcept), typeof(Menu_Dev_TochterfirmaGameComplete),
                typeof(Menu_Stats_Tochterfirma_GameEntwicklungsbericht) })
                yield return AccessTools.Method(type, "Init");
            yield return AccessTools.Method(typeof(Menu_Dev_ChangePlatform), "Update");
            yield return AccessTools.Method(typeof(Item_PubOfferSelect), "SetData");
        }
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            int matches = 0;
            for (int i = 0; i < list.Count; i++)
            {
                yield return list[i];
                if (i >= 2 && list[i].opcode == OpCodes.Conv_I4 &&
                    list[i - 1].opcode == OpCodes.Ldlen &&
                    list[i - 2].operand is FieldInfo &&
                    ((FieldInfo)list[i - 2].operand).DeclaringType == typeof(gameScript) &&
                    ((FieldInfo)list[i - 2].operand).Name == "gamePlatform")
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_4);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Math), "Min", new[] { typeof(int), typeof(int) }));
                    matches++;
                }
            }
            if (matches != 1) throw new InvalidOperationException("Layout de plataformas inesperado: " + matches);
        }
    }

    [HarmonyPatch(typeof(platformScript), "GameIsCompatible")]
    internal static class Compatibility
    {
        static void Postfix(platformScript __instance, gameScript script_, ref bool __result)
        {
            if (!__result)
                __result = __instance.platformCompatible.Any(id => id > 0 && Array.IndexOf(script_.gamePlatform, id) >= 0);
        }
    }

    [HarmonyPatch(typeof(gameScript), "IsKonsoleAbwaertskompatibel")]
    internal static class BackwardCompatibility
    {
        static void Postfix(gameScript __instance, platformScript script_, ref bool __result)
        {
            if (!__result && script_ && script_.hwFeatures[1])
                __result = script_.platformCompatible.Any(id => id > 0 && Array.IndexOf(__instance.gamePlatform, id) >= 0);
        }
    }

    // A separate key in the same ES3 file keeps the base game's packed data intact.
    [HarmonyPatch(typeof(savegameScript), "SaveGames")]
    internal static class SaveExtras
    {
        internal const string Key = "br_sapo_moreplatforms_v1";
        static void Postfix(ES3Writer writer)
        {
            var data = new List<int>();
            foreach (var game in UnityEngine.Object.FindObjectsOfType<gameScript>())
            {
                var ids = game.gamePlatform;
                if (ids == null || ids.Length <= 4 || !ids.Skip(4).Any(x => x >= 0)) continue;
                data.Add(game.myID);
                data.Add(ids.Length - 4);
                data.AddRange(ids.Skip(4));
            }
            writer.Write<int[]>(Key, data.ToArray());
        }
    }

    [HarmonyPatch(typeof(savegameScript), "LoadGames")]
    internal static class LoadExtras
    {
        static void Postfix(ES3Reader reader)
        {
            int[] data = reader.Read<int[]>(SaveExtras.Key, new int[0]);
            // Validate all records before changing any game.
            var records = ExtraData.Decode(data);
            var gamesById = UnityEngine.Object.FindObjectsOfType<gameScript>()
                .GroupBy(g => g.myID).ToDictionary(g => g.Key, g => g.First());
            foreach (var record in records)
            {
                gameScript game;
                if (!gamesById.TryGetValue(record.Key, out game)) continue;
                Plugin.EnsureGame(game, record.Value.Length + 4);
                Array.Copy(record.Value, 0, game.gamePlatform, 4, record.Value.Length);
                game.FindMyPlatforms();
            }
        }
    }
}
