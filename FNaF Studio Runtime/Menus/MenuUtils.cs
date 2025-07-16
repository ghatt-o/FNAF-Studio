using System.Numerics;
using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Data.CRScript;
using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Data.Definitions.GameObjects;
using FNaFStudio_Runtime.Menus.Definitions;
using FNaFStudio_Runtime.Util;
using Raylib_CsLo;

namespace FNaFStudio_Runtime.Menus;

public static class MenuUtils
{
    public static bool SetBackground(string spriteName)
    {
        MenuHandler.MenuReference.Properties.BackgroundImage = spriteName;
        return true;
    }

    public static bool GotoMenu(string menuName)
    {
        if (MenusCore.Menus.TryGetValue(menuName, out var menu))
        {
            Logger.LogAsync("MenuUtils", $"Going to: {menuName}");
            if (MenusCore.ConvertMenuToApi(GameState.Project.Menus[menuName]) is { } newMenu)
                MenusCore.Menus[menuName] = newMenu;
            else
                Logger.LogErrorAsync("MenuUtils", $"Failed to update menu: {menuName}");

            EventManager.KillAllListeners();
            SoundPlayer.KillAll();
            if (GameState.CurrentScene.Name != "Menus")
                RuntimeUtils.Scene.SetScene(SceneType.Menu);
            MenuHandler.MenuReference = menu;
            GameCache.Buttons.Clear();
            SoundPlayer.PlayOnChannel(menu.Properties.BackgroundMusic, true, 1);
            EventManager.RunScript(menu.Code);
            EventManager.TriggerEvent("on_menu_start", []);
            GameState.Clock.Restart();
            return true;
        }

        Logger.LogErrorAsync("MenuUtils", $"Menu {menuName} not found");

        return false;
    }

    public static void ButtonClick(MenuElement element, bool isImage)
    {
        if (GameState.DebugMode)
            Logger.LogAsync("MenuUtils", $"Button '{element.Id}' clicked; IsImage: {isImage}");

        EventManager.TriggerEvent(isImage ? "image_clicked" : "button_clicked", [element.Id]);
    }

    // draw functions

    public static void DrawMenuBackgrounds()
    {
        if (string.IsNullOrEmpty(MenuHandler.MenuReference.Properties.BackgroundImage)) return;

        Raylib.DrawTexture(Cache.GetTexture(MenuHandler.MenuReference.Properties.BackgroundImage), 0, 0, Raylib.WHITE);
    }

    public static class Element
    {
        private static MenuElement GetElementFromId(string id)
        {
            var fod = MenuHandler.MenuReference.Elements.FirstOrDefault(el => el.Id == id);
            if (fod != null) return fod;

            Logger.LogFatalAsync("MenuUtils - Element", "First or Default elements are missing.");
            return new MenuElement(); // not happening
        }

        private static bool SetElementVisibility(string id, bool hidden)
        {
            if (GetElementFromId(id) is not { } newEl) return false;
            newEl.Hidden = hidden;
            return true;

        }

        private static bool SetElementSprite(string id, string sprite)
        {
            if (GetElementFromId(id) is not { Type: "Image" } newEl) return false;
            newEl.Sprite = sprite;
            return true;

        }

        public static bool IsElementSelected(string elementId)
        {
            return GetElementFromId(elementId) switch
            {
                not null => GameCache.Buttons.TryGetValue(elementId, out var btn) && btn.IsHovered,
                _ => false
            };
        }

        public static bool HideElement(string elementId)
        {
            return SetElementVisibility(elementId, true);
        }

        public static bool ShowElement(string elementId)
        {
            return SetElementVisibility(elementId, false);
        }

        public static bool SetSprite(string elementId, string newSprite)
        {
            return SetElementSprite(elementId, newSprite);
        }

        public static bool SetText(string elementId, string text)
        {
            if (GetElementFromId(elementId) is not { } newEl) return false;
            newEl.Text = text;
            return true;

        }

        // draw functions

        public static void DrawMenuElements()
        {
            MenuHandler.MenuReference.Elements.ForEach(el =>
            {
                if (el.Hidden) return;
                var uid = $"{el.Text}-{el.FontSize}";

                Vector2 elPos = new(el.X, el.Y);
                switch (el.Type)
                {
                    case "StaticText":
                        if (GameCache.Texts.TryGetValue(uid, out var text))
                        {
                            text.Draw(elPos);
                        }
                        else
                        {
                            var newText = new Text(el.Text, el.FontSize, el.FontName, Raylib.WHITE);
                            GameCache.Texts[uid] = newText;
                            newText.Draw(elPos);
                        }

                        break;
                    case "Button":
                        if (GameCache.Buttons.TryGetValue(el.Id, out var btn))
                        {
                            btn.Draw(elPos);
                        }
                        else
                        {
                            lock (GameState.ButtonsLock)
                            {
                                var tx = GameCache.Texts.TryGetValue(uid, out var value)
                                ? value
                                : GameCache.Texts[uid] = new Text(el.Text, el.FontSize, el.FontName, Raylib.WHITE);
                                var newBtn = new Button2D(elPos, element: el, text: tx);
                                GameCache.Buttons[el.Id] = newBtn;
                                newBtn.Draw(elPos);
                            }
                        }

                        break;
                    case "Image":
                        if (GameCache.Buttons.TryGetValue(el.Id, out var imgbtn))
                        {
                            imgbtn.Draw(elPos);
                        }
                        else
                        {
                            lock (GameState.ButtonsLock)
                            {
                                var tex = Cache.GetTexture(el.Sprite);
                                var newImgBtn = new Button2D(elPos, element: el, texture: tex);
                                GameCache.Buttons[el.Id] = newImgBtn;
                                newImgBtn.Draw(elPos);
                            }
                        }

                        break;
                    case "Animation":
                        var anim = Cache.GetAnimation(el.Animation);
                        anim.Advance();
                        anim.Draw(elPos);
                        break;
                    default:
                        Logger.LogFatalAsync("MenuHandler", $"Unimplemented Element Type: {el.Type}");
                        break;
                }
            });
        }
    }
}