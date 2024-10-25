using FNAFStudio_Runtime_RCS.Data.CRScript;
using FNAFStudio_Runtime_RCS.Menus;
using FNAFStudio_Runtime_RCS.Menus.Definitions;
using Raylib_CsLo;
using System.Numerics;

namespace FNAFStudio_Runtime_RCS.Data.Definitions.GameObjects
{
    public class Button2D
    {
        public Rectangle Bounds { get; private set; }
        public bool IsHoverable { get; private set; } = true;
        public bool IsHovered { get; private set; }
        public bool IsClicked { get; private set; }
        public bool IsClickable { get; private set; } = true;
        public bool IsImage { get; private set; }
        public MenuElement? Element { get; private set; }
        public GameJson.OfficeObject? Object { get; private set; }

        private Text? Text;
        private Texture? Texture;
        private readonly Func<Task>? onHoverAsync;
        private Func<Task>? onClickAsync;
        private Func<Task>? onReleaseAsync;

        public static readonly List<bool> DisabledClicks = [false, false, false, false];

        public Button2D(Vector2 position, GameJson.OfficeObject? obj = null, MenuElement? element = null, Texture? texture = null, Text? text = null, Func<Task>? onHover = null, Func<Task>? onClick = null, Func<Task>? onRelease = null)
        {
            Texture? tex = texture ?? GetTextureSafe(element?.Sprite ?? obj?.Sprite);
            Bounds = CreateBounds(position, tex, text);
            Text = text;
            Element = element;
            Object = obj;
            IsImage = texture.HasValue;
            Texture = texture;
            onHoverAsync = onHover;
            onClickAsync = onClick;
            onReleaseAsync = onRelease;
        }

        private static Rectangle CreateBounds(Vector2 position, Texture? texture, Text? text)
        {
            const float padding = 5.0f;

            if (texture.HasValue)
            {
                return new Rectangle(position.X - padding, position.Y - padding, texture.Value.width + 2 * padding, texture.Value.height + 2 * padding);
            }

            if (text != null)
            {
                Rectangle bounds = text.GetBounds(position);
                return new Rectangle(bounds.X - padding, bounds.Y - padding, bounds.width + 2 * padding, bounds.height + 2 * padding);
            }

            throw new ArgumentException("Insufficient arguments to create bounds");
        }

        private static Texture? GetTextureSafe(string? sprite)
        {
            if (!string.IsNullOrEmpty(sprite))
            {
                Texture tex = Cache.GetTexture(sprite);
                return tex;
            }
            return null;
        }

        public void OnClickAsync(Func<Task> onClick)
        {
            onClickAsync = onClick;
        }

        public void OnReleaseAsync(Func<Task> onRelease)
        {
            onReleaseAsync = onRelease;
        }

        public async Task UpdateAsync(float xOffset)
        {
            Rectangle Temp = Bounds;
            Temp.X -= xOffset;
            Vector2 mousePosition = Raylib.GetMousePosition();
            IsHovered = Raylib.CheckCollisionPointRec(mousePosition, Temp);
            IsClicked = IsHovered && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT);

            if (IsHovered)
            {
                await HandleHoverAsync();
            }

            if (IsClicked)
            {
                await HandleClickAsync();
            }

            // Handle mouse button release
            if (IsHovered && Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
            {
                await HandleReleaseAsync();
            }
        }

        public async Task HandleHoverAsync()
        {
            var id = Element?.ID ?? Object?.ID;
            if (id != null && GameState.SelectedButtonID != id)
            {
                GameState.SelectedButtonID = id;
                if (onHoverAsync != null)
                    await onHoverAsync();

                // This already handles menu-specific logic
                if (Element != null)
                {
                    EventManager.TriggerEvent("any_button_selected", []);
                    EventManager.TriggerEvent("button_selected", [Element.ID]);
                }
            }
        }

        private async Task HandleClickAsync()
        {
            if (onClickAsync != null)
                await onClickAsync();

            if (Element != null)
            {
                await MenuUtils.ButtonClick(Element, IsImage);
            }
        }

        private async Task HandleReleaseAsync()
        {
            if (onReleaseAsync != null)
                await onReleaseAsync();
        }

        public void Draw(Vector2 position, bool on = false)
        {
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
            string? sprite = Object?.Sprite;
            if (Object != null && on)
                sprite = Object.On_Sprite;
            sprite ??= Element?.Sprite;

            if (sprite != null)
                Texture = Cache.GetTexture(sprite);
            
            if (Texture.HasValue)
                Raylib.DrawTextureEx((Texture)Texture, position, rotation: 0, scale: 1, Raylib.WHITE);
        }

        private void DrawTextObject(Vector2 position)
        {
            if (Object != null && Object.Text != null && Text != null && Object.Text != Text.Content)
            {
                string uid = Object.Text ?? "";
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
                    Raylib.DrawTextEx(Text!.Font, ">> ", new Vector2(position.X - Element.FontSize * 1.5f, position.Y), Text.FontSize, 1, Raylib.WHITE);
                MenusCore.ArrowIn = true;
            }
            else
            {
                MenusCore.ArrowIn = false;
            }

            if (Element != null && Text != null && Element.Text != Text.Content)
            {
                string uid = $"{Element.Text}-{Element.FontSize}";
                Text = new Text(Element.Text, Element.FontSize, Element.FontName, Raylib.WHITE);
                GameCache.Texts[uid] = Text;
            }

            Text?.Draw(position);
        }
    }
}