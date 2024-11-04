using System.Numerics;
using FNAFStudio_Runtime_RCS.Data;
using FNAFStudio_Runtime_RCS.Data.CRScript;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using FNAFStudio_Runtime_RCS.Data.Definitions.GameObjects;
using FNAFStudio_Runtime_RCS.Menus.Definitions;
using FNAFStudio_Runtime_RCS.Util;
using Raylib_CsLo;

namespace FNAFStudio_Runtime_RCS.Menus;

public static class MenuUtils
{
    public static bool SetBackground(string spriteName)
    {
        MenuHandler.menuReference.Properties.BackgroundImage = spriteName;
        return true;
    }

    public static bool GotoMenu(string MenuName)
    {
        if (MenusCore.Menus.TryGetValue(MenuName, out var menu))
        {
            Logger.LogAsync("MenuUtils", $"Going to: {MenuName}");
            if (MenusCore.ConvertMenuToAPI(GameState.Project.Menus[MenuName]) is { } newMenu)
                MenusCore.Menus[MenuName] = newMenu;
            else
                Logger.LogErrorAsync("MenuUtils", $"Failed to update menu: {MenuName}");

            EventManager.KillAllListeners();
            SoundPlayer.KillAllAsync().Wait();
            if (GameState.CurrentScene.Name != "Menus")
                RuntimeUtils.Scene.SetScene(SceneType.Menu);
            MenusCore.Menu = MenuName;
            MenuHandler.menuReference = menu;
            GameCache.Buttons.Clear();
            SoundPlayer.PlayOnChannelAsync(menu.Properties.BackgroundMusic, true, 1).Wait();
            EventManager.RunScript(menu.Code);
            EventManager.TriggerEvent("on_menu_start", []);
            GameState.Clock.Restart();
            return true;
        }

        Logger.LogErrorAsync("MenuUtils", $"Menu {MenuName} not found");

        return false;
    }

    public static async Task ButtonClick(MenuElement element, bool IsImage)
    {
        if (GameState.DebugMode)
            await Logger.LogAsync("MenuUtils", $"Button '{element.ID}' clicked; IsImage: {IsImage}");

        // Dont check if the mouse was pressed here because this function
        // only triggers once the mouse is clicked, checks are useless
        EventManager.TriggerEvent(IsImage ? "image_clicked" : "button_clicked", [element.ID]);
    }

    // draw functions

    public static void DrawMenuBackgrounds()
    {
        if (string.IsNullOrEmpty(MenuHandler.menuReference.Properties.BackgroundImage)) return;

        Raylib.DrawTexture(Cache.GetTexture(MenuHandler.menuReference.Properties.BackgroundImage), 0, 0, Raylib.WHITE);
    }

    public static class Element
    {
        public static MenuElement GetElementFromID(string id)
        {
            var fod = MenuHandler.menuReference.Elements.FirstOrDefault(el => el.ID == id);
            if (fod != null) return fod;

            Logger.LogFatalAsync("MenuUtils - Element", "fod is null.");
            return new MenuElement(); // not happening
        }

        public static bool SetElementVisibility(string id, bool hidden)
        {
            if (GetElementFromID(id) is { } newEl)
            {
                newEl.Hidden = hidden;
                return true;
            }

            return false;
        }

        public static bool SetElementSprite(string id, string sprite)
        {
            if (GetElementFromID(id) is { } newEl && newEl.Type == "Image")
            {
                newEl.Sprite = sprite;
                return true;
            }

            return false;
        }

        public static bool IsElementSelected(string elementID)
        {
            return GetElementFromID(elementID) switch
            {
                not null => GameCache.Buttons.TryGetValue(elementID, out var btn) && btn.IsHovered,
                _ => false
            };
        }

        public static bool HideElement(string elementID)
        {
            return SetElementVisibility(elementID, true);
        }

        public static bool ShowElement(string elementID)
        {
            return SetElementVisibility(elementID, false);
        }

        public static bool SetSprite(string elementID, string newSprite)
        {
            return SetElementSprite(elementID, newSprite);
        }

        public static bool SetText(string elementID, string text)
        {
            if (GetElementFromID(elementID) is { } newEl)
            {
                newEl.Text = text;
                return true;
            }

            return false;
        }

        // draw functions

        public static void DrawMenuElements()
        {
            MenuHandler.menuReference.Elements.ForEach(el =>
            {
                if (el.Hidden) return;
                var uid = $"{el.Text}-{el.FontSize}";

                Vector2 ElPos = new(el.X, el.Y);
                switch (el.Type)
                {
                    case "StaticText":
                        if (GameCache.Texts.TryGetValue(uid, out var text))
                        {
                            text.Draw(ElPos);
                        }
                        else
                        {
                            var newText = new Text(el.Text, el.FontSize, el.FontName, Raylib.WHITE);
                            GameCache.Texts[uid] = newText;
                            newText.Draw(ElPos);
                        }

                        break;
                    case "Button":
                        if (GameCache.Buttons.TryGetValue(el.ID, out var btn))
                        {
                            btn.Draw(ElPos);
                        }
                        else
                        {
                            var tx = GameCache.Texts.TryGetValue(uid, out var value)
                                ? value
                                : GameCache.Texts[uid] = new Text(el.Text, el.FontSize, el.FontName, Raylib.WHITE);
                            var newBtn = new Button2D(ElPos, element: el, text: tx);
                            GameCache.Buttons[el.ID] = newBtn;
                            newBtn.Draw(ElPos);
                        }

                        break;
                    case "Image":
                        if (GameCache.Buttons.TryGetValue(el.ID, out var imgbtn))
                        {
                            imgbtn.Draw(ElPos);
                        }
                        else
                        {
                            var tex = Cache.GetTexture(el.Sprite);
                            var newImgBtn = new Button2D(ElPos, element: el, texture: tex);
                            GameCache.Buttons[el.ID] = newImgBtn;
                            newImgBtn.Draw(ElPos);
                        }

                        break;
                    case "Animation":
                        var anim = Cache.GetAnimation(el.Animation);
                        anim.Advance();
                        anim.Draw(ElPos);
                        break;
                    default:
                        Logger.LogFatalAsync("MenuHandler", $"Unimplemented Element Type: {el.Type}");
                        break;
                }
            });
        }
    }
}