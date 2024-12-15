using System.Numerics;
using FNaFStudio_Runtime.Data.CRScript;
using FNaFStudio_Runtime.Menus;
using FNaFStudio_Runtime.Menus.Definitions;
using Raylib_CsLo;

namespace FNaFStudio_Runtime.Data.Definitions.GameObjects;

public class Button2D
{
    public static readonly List<bool> DisabledClicks = [false, false, false, false];
    private Action? onHover;
    private Action? onUnHover;
    private Action? onClick;
    private Action? onRelease;

    private Text? Text;
    private Texture? Texture;

    public Button2D(Vector2 position, GameJson.OfficeObject? obj = null, MenuElement? element = null, string? id = "", bool IsMovable = true,
        bool IsVisible = true, Texture? texture = null, Text? text = null, Action? onHover = null, Action? onClick = null,
        Action? onRelease = null)
    {
        var tex = texture ?? GetTextureSafe(element?.Sprite ?? obj?.Sprite);
        ID = id ?? Element?.ID ?? Object?.ID ?? "";
        Bounds = CreateBounds(position, tex, text);
        Text = text;
        Element = element;
        Object = obj;
        IsImage = texture.HasValue;
        Texture = texture;
        this.onHover = onHover;
        this.onClick = onClick;
        this.onRelease = onRelease;
        this.IsMovable = IsMovable;
        this.IsVisible = IsVisible;
    }

    public string ID { get; private set; } = string.Empty;
    public bool IsMovable { get; set; }
    public bool IsVisible { get; set; }
    public Rectangle Bounds { get; }
    public bool IsHoverable { get; private set; } = true;
    public bool IsHovered { get; private set; }
    public bool IsClicked { get; private set; }
    public bool IsClickable { get; private set; } = true;
    public bool IsImage { get; }
    public MenuElement? Element { get; }
    public GameJson.OfficeObject? Object { get; }

    private static Rectangle CreateBounds(Vector2 position, Texture? texture, Text? text)
    {
        const float padding = 5.0f;

        if (texture.HasValue)
            return new Rectangle(position.X - padding, position.Y - padding, texture.Value.width + 2 * padding,
                texture.Value.height + 2 * padding);

        if (text != null)
        {
            var bounds = text.GetBounds(position);
            return new Rectangle(bounds.X - padding, bounds.Y - padding, bounds.width + 2 * padding,
                bounds.height + 2 * padding);
        }

        throw new ArgumentException("Insufficient arguments to create bounds");
    }

    private static Texture? GetTextureSafe(string? sprite)
    {
        if (!string.IsNullOrEmpty(sprite))
        {
            var tex = Cache.GetTexture(sprite);
            return tex;
        }

        return null;
    }

    public void OnHover(Action onHover)
    {
        this.onHover = onHover;
    }

    public void OnUnHover(Action onHover)
    {
        this.onUnHover = onHover;
    }

    public void OnClick(Action onClick)
    {
        this.onClick = onClick;
    }

    public void OnRelease(Action onRelease)
    {
        this.onRelease = onRelease;
    }

    public void Update(float xOffset)
    {
        if (!IsVisible) return;

        var Temp = Bounds;
        if (IsMovable) Temp.X -= xOffset;
        var mousePosition = Raylib.GetMousePosition();
        IsHovered = Raylib.CheckCollisionPointRec(mousePosition, Temp);
        IsClicked = IsHovered && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT);

        if (IsHovered) HandleHover();
        else HandleUnHover();

        if (IsClicked) HandleClick();

        if (IsHovered && Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT)) HandleRelease();
    }

    private void HandleHover()
    {
        if (ID != null && GameState.SelectedButtonID != ID)
        {
            GameState.SelectedButtonID = ID;
            onHover?.Invoke();

            // This already handles menu-specific logic
            if (Element != null)
            {
                EventManager.TriggerEvent("any_button_selected", []);
                EventManager.TriggerEvent("button_selected", [Element.ID]);
            }
        }
    }

    private void HandleUnHover()
    {
        if (ID != null && GameState.SelectedButtonID == ID)
        {
            GameState.SelectedButtonID = string.Empty;

            onUnHover?.Invoke();

            // This already handles menu-specific logic
            if (Element != null)
            {
                EventManager.TriggerEvent("any_button_deselected", []);
                EventManager.TriggerEvent("button_deselected", [Element.ID]);
            }
        }
    }

    private void HandleClick()
    {
        onClick?.Invoke();

        if (Element != null) MenuUtils.ButtonClick(Element, IsImage);
    }

    private void HandleRelease()
    {
        onRelease?.Invoke();
    }

    public void Draw(Vector2 position, bool on = false)
    {
        if (!IsVisible) return;

        if (IsImage)
        {
            DrawImage(position, on);
        }
        else if (Text != null)
        {
            if (Element != null)
                DrawTextElement(position);
            else
                DrawTextObject(position);
        }
    }

    private void DrawImage(Vector2 position, bool on)
    {
        var sprite = Object?.Sprite;
        if (Object != null && on)
            sprite = Object.On_Sprite;
        sprite ??= Element?.Sprite;

        if (sprite != null)
            Texture = Cache.GetTexture(sprite);

        if (Texture.HasValue)
            Raylib.DrawTextureEx((Texture)Texture, position, 0, 1, Raylib.WHITE);
    }

    private void DrawTextObject(Vector2 position)
    {
        if (Object != null && Object.Text != null && Text != null && Object.Text != Text.Content)
        {
            var uid = Object.Text ?? "";
            Text = new Text(Object.Text ?? "", 36, "Arial", Raylib.WHITE);
            GameCache.Texts[uid] = Text;
        }

        Text?.Draw(position);
    }

    private void DrawTextElement(Vector2 position)
    {
        if (IsHovered && MenuHandler.menuReference.Properties.ButtonArrows && !MenusCore.ArrowIn)
        {
            if (Element != null)
                Raylib.DrawTextEx(Text!.Font, ">> ", new Vector2(position.X - Element.FontSize * 1.5f, position.Y),
                    Text.FontSize, 1, Raylib.WHITE);
            MenusCore.ArrowIn = true;
        }
        else
        {
            MenusCore.ArrowIn = false;
        }

        if (Element != null && Text != null && Element.Text != Text.Content)
        {
            var uid = $"{Element.Text}-{Element.FontSize}";
            Text = new Text(Element.Text, Element.FontSize, Element.FontName, Raylib.WHITE);
            GameCache.Texts[uid] = Text;
        }

        Text?.Draw(position);
    }
}