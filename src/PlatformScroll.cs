using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MorePlatforms
{
    public sealed class PlatformScroll : MonoBehaviour
    {
        private Menu_DevGame menu;
        private ScrollRect scroll;
        private RectTransform content;
        private Scrollbar bar;
        private readonly List<RectTransform> cards = new List<RectTransform>();
        private readonly List<ExtraCard> extraCards = new List<ExtraCard>();
        private readonly Vector3[] corners = new Vector3[4];
        private float pitchX, pitchY, firstX, firstY, viewportHeight;
        private int shownCount = -1;
        private float nextRefresh;
        private static readonly int[] UiBases = { 39, 43, 47, 51, 55, 168, 180, 205 };
        private static readonly System.Reflection.FieldInfo Gui =
            AccessTools.Field(typeof(Menu_DevGame), "guiMain_");

        private sealed class ExtraCard
        {
            internal int Slot;
            internal readonly Dictionary<int, GameObject> Ui = new Dictionary<int, GameObject>();
            internal Button Select;
            internal Button Remove;
        }

        internal void Build(Menu_DevGame owner)
        {
            if (scroll) return;
            menu = owner;
            int[] indices = { 140, 33, 34, 35 };
            var parent = menu.uiObjects[140].transform.parent as RectTransform;
            var removers = new RectTransform[4];
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (int index in indices)
            {
                var card = menu.uiObjects[index].GetComponent<RectTransform>();
                if (card.parent != parent) throw new InvalidOperationException("Layout de plataformas desconhecido.");
                cards.Add(card);
                BoundsInParent(card, parent, ref min, ref max);
            }
            for (int i = 0; i < 4; i++)
            {
                removers[i] = parent.Find("Button_RemovePlatform" + (i + 1)) as RectTransform;
                if (!removers[i]) throw new InvalidOperationException("Botao de remover plataforma ausente.");
                BoundsInParent(removers[i], parent, ref min, ref max);
            }
            Vector2 first = parent.InverseTransformPoint(cards[0].TransformPoint(cards[0].rect.center));
            Vector2 second = parent.InverseTransformPoint(cards[1].TransformPoint(cards[1].rect.center));
            Vector2 third = parent.InverseTransformPoint(cards[2].TransformPoint(cards[2].rect.center));
            pitchX = second.x - first.x;
            pitchY = first.y - third.y;
            if (pitchX <= 0 || pitchY <= 0) throw new InvalidOperationException("Grade de plataformas desconhecida.");
            // Use some of the original inter-column gap for the scrollbar so it
            // stays inside the existing platform panel, without shrinking cards.
            pitchX -= 14;
            min -= Vector2.one * 3;
            max += Vector2.one * 3;
            firstX = first.x - min.x;
            firstY = max.y - first.y;
            viewportHeight = max.y - min.y;
            float width = max.x - min.x - 14;

            var root = new GameObject("MorePlatformsScroll", typeof(RectTransform), typeof(ScrollRect));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(min.x - parent.rect.xMin, max.y - parent.rect.yMax);
            rect.sizeDelta = new Vector2(width + 17, viewportHeight);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            var viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = new Vector2(-17, 0);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport.transform, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0, 1);
            content.sizeDelta = new Vector2(0, viewportHeight);

            // Remove buttons are siblings in the original prefab. Keep their world
            // position while making each travel with its own card.
            for (int i = 0; i < 4; i++)
            {
                removers[i].SetParent(cards[i], true);
                Vector3 local = removers[i].localPosition;
                removers[i].anchorMin = removers[i].anchorMax = new Vector2(0.5f, 0.5f);
                removers[i].localPosition = local;
                PositionCard(cards[i], i);
            }

            // Copy the existing styled scrollbar instead of introducing a new skin.
            var sourceBar = menu.uiObjects[146].GetComponent<Dropdown>().template.GetComponentInChildren<Scrollbar>(true);
            if (!sourceBar) throw new InvalidOperationException("Scrollbar original nao encontrado.");
            bar = UnityEngine.Object.Instantiate(sourceBar, root.transform, false);
            bar.name = "PlatformScrollbar";
            bar.onValueChanged = new Scrollbar.ScrollEvent();
            bar.direction = Scrollbar.Direction.BottomToTop;
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(1, 0);
            barRect.anchorMax = Vector2.one;
            barRect.pivot = new Vector2(1, 0.5f);
            barRect.offsetMin = new Vector2(-14, 0);
            barRect.offsetMax = Vector2.zero;

            scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            SetVisibleCount(4);
        }

        private void BoundsInParent(RectTransform rect, Transform parent, ref Vector2 min, ref Vector2 max)
        {
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Vector2 point = parent.InverseTransformPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }

        private void PositionCard(RectTransform card, int index)
        {
            Vector2 size = card.rect.size;
            card.SetParent(content, false);
            card.anchorMin = card.anchorMax = new Vector2(0, 1);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = size;
            card.anchoredPosition = new Vector2(firstX + (index % 2) * pitchX,
                -firstY - (index / 2) * pitchY);
        }

        private static GameObject MatchChild(Transform sourceRoot, Transform copyRoot, Transform source)
        {
            var path = new Stack<int>();
            while (source != sourceRoot)
            {
                if (!source) throw new InvalidOperationException("Elemento fora do cartao.");
                path.Push(source.GetSiblingIndex());
                source = source.parent;
            }
            while (path.Count > 0) copyRoot = copyRoot.GetChild(path.Pop());
            return copyRoot.gameObject;
        }

        private void AddCard(int slot)
        {
            RectTransform source = cards[3];
            var copy = UnityEngine.Object.Instantiate(source, content, false);
            copy.name = "Button_Platform" + (slot + 1);
            PositionCard(copy, slot);
            var card = new ExtraCard { Slot = slot, Select = copy.GetComponent<Button>() };
            foreach (int ui in UiBases)
                card.Ui.Add(ui, MatchChild(source, copy, menu.uiObjects[ui + 3].transform));
            MatchChild(source, copy, menu.uiObjects[38].transform).SetActive(false);
            card.Remove = copy.Find("Button_RemovePlatform4").GetComponent<Button>();
            card.Remove.name = "RemoveExtraPlatform";
            card.Select.onClick = new Button.ButtonClickedEvent();
            card.Select.onClick.AddListener(delegate { menu.BUTTON_Platform(slot); });
            card.Remove.onClick = new Button.ButtonClickedEvent();
            card.Remove.onClick.AddListener(delegate { menu.BUTTON_RemovePlatform(slot); });
            cards.Add(copy);
            extraCards.Add(card);
            DrawCard(card);
        }

        internal void SetVisibleCount(int count)
        {
            if (!scroll) return;
            count = Math.Max(4, Math.Min(16, count));
            while (cards.Count < count) AddCard(cards.Count);
            for (int i = 4; i < cards.Count; i++) cards[i].gameObject.SetActive(i < count);
            content.sizeDelta = new Vector2(0, viewportHeight + ((count + 1) / 2 - 2) * pitchY);
            bar.gameObject.SetActive(count > 4);
            scroll.vertical = count > 4;
            if (shownCount != count)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1;
                content.anchoredPosition = Vector2.zero;
                shownCount = count;
            }
        }

        internal void Sync(bool resetScroll)
        {
            if (!scroll || menu.g_GamePlatform == null) return;
            SetVisibleCount(Plugin.Allowed(menu) ? menu.g_GamePlatform.Length : 4);
            foreach (var card in extraCards)
                if (card.Slot < shownCount) DrawCard(card);
            if (resetScroll)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1;
                content.anchoredPosition = Vector2.zero;
            }
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.25f;
            Sync(false);
        }

        private void DrawCard(ExtraCard card)
        {
            int id = menu.g_GamePlatform != null && card.Slot < menu.g_GamePlatform.Length ?
                menu.g_GamePlatform[card.Slot] : -1;
            var obj = id >= 0 ? GameObject.Find("PLATFORM_" + id) : null;
            var platform = obj ? obj.GetComponent<platformScript>() : null;
            card.Ui[39].GetComponent<Text>().text = platform ? platform.GetName() :
                "Plataforma " + (card.Slot + 1);
            card.Ui[43].SetActive(platform);
            card.Ui[51].GetComponent<Text>().text = platform ? platform.tech.ToString() : "-";
            card.Ui[55].GetComponent<Text>().text = platform ? platform.GetMarktanteilString() : "";
            card.Ui[180].SetActive(platform && platform.internet);
            card.Ui[168].GetComponent<Image>().sprite = platform ? platform.GetComplexSprite() : null;
            card.Ui[205].GetComponent<Image>().sprite = platform ? platform.GetTypSprite() : null;
            card.Ui[168].SetActive(platform);
            card.Ui[205].SetActive(platform);
            card.Ui[205].GetComponent<tooltip>().c = platform ? platform.GetTypString() : "";
            card.Ui[43].GetComponent<tooltip>().c = platform ? platform.GetTooltip() : "";
            if (platform) platform.SetPic(card.Ui[43]);
            var gui = Gui.GetValue(menu) as GUI_Main;
            if (gui) gui.DrawStars(card.Ui[47], platform ? platform.erfahrung : 0);
            card.Select.interactable = Plugin.Allowed(menu);
            card.Remove.interactable = Plugin.Allowed(menu) && id >= 0;
        }
    }

    [HarmonyPatch(typeof(Menu_DevGame), "SetPlatform")]
    internal static class RefreshPlatformCards
    {
        static void Postfix(Menu_DevGame __instance)
        {
            var view = __instance.GetComponent<PlatformScroll>();
            if (view) view.Sync(false);
        }
    }
}
