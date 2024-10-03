using FNAFStudio_Runtime_RCS.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raylib_CsLo;

namespace FNAFStudio_Runtime_RCS.Data.Definitions;

public class GameJson
{
    public class Game
    {
        public Dictionary<string, Animatronic> Animatronics { get; set; } = [];
        public Dictionary<string, Camera> Cameras { get; set; } = [];
        public GameInfo GameInfo { get; set; } = new();
        public Dictionary<string, Menu> Menus { get; set; } = [];
        public Dictionary<string, Office> Offices { get; set; } = [];
        public Office Office { get; set; } = new();
        public Sounds Sounds { get; set; } = new();
        public List<string> Loaded_extensions { get; set; } = [];

        [JsonIgnore] public string Name { get; set; } = string.Empty;
        [JsonIgnore] public Dictionary<string, List<Code>> OfficeScripts { get; set; } = [];
        [JsonIgnore] public Dictionary<string, List<AnimationJson>> Animations { get; set; } = [];

        public static Game Load(string inputJsonPath)
        {
            var content = File.ReadAllText(inputJsonPath);
            var serializerSettings = new JsonSerializerSettings
            {
                Converters =
                [
                    new ElementListConverter(),
                    new MultiTypeConverter(),
                    new CamSpriteConverter(),
                ]
            };
            Game? gameJson = JsonConvert.DeserializeObject<Game>(content, serializerSettings);
            if (gameJson != null)
            {

                var scriptsPath = inputJsonPath.Replace("game.json", "scripts");
                var scripts = new Dictionary<string, List<Code>>();

                foreach (var scriptFile in Directory.EnumerateFiles(scriptsPath, "*.fescript"))
                {
                    var scriptContent = File.ReadAllText(scriptFile);
                    var codes = JsonConvert.DeserializeObject<List<Code>>(scriptContent) ?? [];
                    var key = Path.GetFileNameWithoutExtension(scriptFile);
                    scripts.Add(key, codes);
                }

                gameJson.OfficeScripts = scripts;
                return gameJson;
            }
            throw new JsonSerializationException("Null game json.");
        }
    }

    #region Nested Classes

    public class Animatronic
    {
        public List<int> AI { get; set; } = [];
        public bool IgnoreMask { get; set; } = false;
        public List<string> Jumpscare { get; set; } = [];
        public List<PathNode> Path { get; set; } = [];
        public int MoveTime { get; internal set; }
        public string? State { get; internal set; }
        public bool Phantom { get; set; } = false;
        public bool Script { get; set; } = false; // call a certain event on jumpscare

        public bool Moving;
        public bool Paused;
        public int CurAI;
        public int PathIndex;
        public string? curCam;
        public PathNode? CurPath;
        public PathNode? PrevPath;
    }

    public class PathNode
    {
        public string ID { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Chance { get; set; } = 0;
        public string State { get; set; } = string.Empty;
        public List<PathNode> Path { get; set; } = [];
        public string CamID { get; set; } = string.Empty;
    }

    public class CamUI
    {
        public Dictionary<string, CamSprite> Buttons { get; set; } = [];
        public Dictionary<string, CamSprite> Sprites { get; set; } = [];
        public List<int> MusicBox { get; set; } = [];
        public bool UI { get; set; }
        public bool Blip { get; set; }
    }

    [JsonConverter(typeof(MultiTypeConverter))]
    public class MultiType
    {
        public MultiType(string value)
        {
            StrValue = value;
        }

        public MultiType(int value)
        {
            IntValue = value;
        }

        public string? StrValue { get; set; }
        public int? IntValue { get; set; }
    }

    public class GameInfo
    {
        public bool Fullscreen = false;
        public int Height = 720;

        // static effect Settings
        public int Opacity = 30;
        public string Icon { get; set; } = string.Empty; // game icon
        public string ID { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Width { get; set; } = 1280;
        public bool Invert { get; set; } = false;
        public bool Vhs { get; set; } = false; // additional effect

        // unused...
        public int Style { get; set; } = 0;
    }

    public class Menu
    {
        public List<Code> Code { get; set; } = [];

        [JsonConverter(typeof(ElementListConverter))]
        public List<Element> Elements { get; set; } = [];

        public Properties Properties { get; set; } = new();
    }

    public class Code
    {
        public bool Negated { get; set; } = false;
        public List<string> Args { get; set; } = [];
        public string Block { get; set; } = string.Empty;
        public List<Code> Subcode { get; set; } = [];
    }

    public class Element
    {
        public int Blue { get; set; } = 0;
        public int Red { get; set; } = 0;
        public int Green { get; set; } = 0;
        public string Fontname { get; set; } = string.Empty;
        public int Fontsize { get; set; } = 0;
        public bool Hidden { get; set; } = false;
        public string ID { get; set; } = string.Empty;
        public string Animation { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Sprite { get; set; } = string.Empty;
        public string Animatronic { get; set; } = string.Empty;
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
    }

    public class Properties
    {
        public string BackgroundImage { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
        public string BackgroundMusic { get; set; } = string.Empty;
        public bool ButtonArrows { get; set; } = false;
        public bool FadeIn { get; set; } = false;
        public bool FadeOut { get; set; } = false;
        public int FadeSpeed { get; set; } = 0;
        public bool Panorama { get; set; } = false;
        public string ButtonArrowStr { get; set; } = string.Empty;
        public string ButtonArrowColor { get; set; } = string.Empty;
        public string ButtonArrowFont { get; set; } = string.Empty;
        public bool? StaticEffect { get; set; } = true;
        public bool MenuScroll { get; set; }
    }

    public class Office
    {
        public List<OfficeLayer> Layers = [];
        public Animations Animations { get; set; } = new();
        public List<OfficeObject> Objects { get; set; } = [];
        public Power Power { get; set; } = new();
        public Dictionary<string, string> States { get; set; } = [];

        [JsonProperty("ui_buttons")]
        public Dictionary<string, UIButton> UIButtons { get; set; } = [];
        [JsonProperty("uibuttons")]
        public Uibuttons? OldUIButtons { get; set; } = new();

        // properties
        public bool Flashlight { get; set; } = false;
        public bool Mask { get; set; } = false;
        public bool Panorama { get; set; } = true;
        public bool Toxic { get; set; } = false;
        public bool Battery { get; set; } = false;
        public bool Display { get; set; } = true;
        public bool Display2 { get; set; } = true;
        public string Keybind { get; set; } = string.Empty;
        public bool Fast { get; set; } = false;
        public bool Strong { get; set; } = false;
        public string NightFont { get; set; } = "Consolas";
        public bool Toggle { get; set; } = false;
        public bool Perspective { get; set; } = true;
        public bool Time { get; set; } = true;
        public bool Camera { get; set; } = true;
        public int EndingTime { get; set; } = 6;
        public string DisplayName { get; set; } = "Night";
        public string? TextFont { get; set; }
    }

    public class OfficeLayer
    {
        public List<OfficeObject> Objects = [];
        public bool Visible { get; set; } = true;
    }

    public class UIButton
    {
        public Input? Input { get; set; }
        public UI? UI { get; set; }
    }

    public class Input // camera and mask uses this
    {
        public string? Image { get; set; }
        public List<int>? Position { get; set; }
    }

    public class UI
    {
        public bool IsToxic { get; set; } = false;
        public string? Text { get; set; }
    }

    public class Animations
    {
        public string Camera { get; set; } = string.Empty;
        public string Mask { get; set; } = string.Empty;
        public string Powerout { get; set; } = string.Empty;
    }

    public class OfficeObject
    {
        public bool Clickstyle { get; set; } = false;
        public string ID { get; set; } = string.Empty;
        public string On_Sprite { get; set; } = string.Empty;
        public List<int> Position { get; set; } = [];
        public string CloseSound { get; set; } = string.Empty;
        public string OpenSound { get; set; } = string.Empty;
        public string Animation { get; set; } = string.Empty;
        public string Sound { get; set; } = string.Empty;
        public string Sprite { get; set; } = string.Empty;
        public List<int> Trigger { get; set; } = [];
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty; // "r,g,b" split by ,
        public int Width { get; set; } // quick surface only (for now)
        public int Height { get; set; } // quick surface only (for now)
    }

    public class Power
    {
        public string Animatronic { get; set; } = string.Empty;
        public bool Enabled { get; set; } = false;
        public int StartingLevel { get; set; } = 0;
        public bool Ucn { get; set; } = false;
    }

    public class Uibuttons
    {
        public CameraPanel Camera { get; set; } = new();
        public MaskPanel Mask { get; set; } = new();
    }

    public class CameraPanel
    {
        public string Image { get; set; } = string.Empty;
        public List<int> Position { get; set; } = [];
    }

    public class MaskPanel
    {
        public string Image { get; set; } = string.Empty;
        public List<int> Position { get; set; } = [];
    }

    public class CamSprite
    {
        public bool Visible = true;
        public bool hovered;
        public Rectangle bounds;
        public string? Sprite { get; set; } = "";
        public string? Selected { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public string? ID { get; set; } = "";

    }

    public class Camera
    {
        public string CurState = "Default";
        public Dictionary<string, CamSprite> Buttons { get; set; } = [];
        public List<int> MusicBox { get; set; } = [];
        public Dictionary<string, CamSprite> Sprites { get; set; } = [];
        public List<string> VisibleSprites { get; set; } = [];
        public Dictionary<string, string> States { get; set; } = [];
        public bool Blip { get; set; }
        public bool Panorama { get; set; }
        public bool UI { get; set; }
        public bool Static { get; set; }
    }

    public class Sounds
    {
        public string Ambience { get; set; } = string.Empty;
        public List<string> AnimatronicMove { get; set; } = [];
        public string Blip { get; set; } = string.Empty;
        public string Camdown { get; set; } = string.Empty;
        public string Camup { get; set; } = string.Empty;
        public string Flashlight { get; set; } = string.Empty;
        public string MaskBreathing { get; set; } = string.Empty;
        public string Maskoff { get; set; } = string.Empty;
        public string Maskon { get; set; } = string.Empty;
        public string MaskToxic { get; set; } = string.Empty;
        public string MusicBoxRunOut { get; set; } = string.Empty;
        public List<string> PhoneCalls { get; set; } = [];
        public string Powerout { get; set; } = string.Empty;
        public string SignalInterrupted { get; set; } = string.Empty;
        public string Stare { get; set; } = string.Empty;
    }

    public class AnimationJson
    {
        public int Duration { get; set; } = 0;
        public string Sprite { get; set; } = string.Empty;
    }

    #endregion

    #region JsonConverters

    public class MultiTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MultiType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value != null)
            {
                return reader.TokenType switch
                {
                    JsonToken.String => new MultiType((string)reader.Value),
                    JsonToken.Integer => new MultiType(Convert.ToInt32(reader.Value)),
                    _ => throw new JsonSerializationException("Invalid type for MultiType")
                };
            }
            throw new JsonSerializationException("Null reader value");
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value != null)
            {
                var multiType = (MultiType)value;
                if (multiType.IntValue.HasValue)
                    writer.WriteValue(multiType.IntValue);
                else if (!string.IsNullOrEmpty(multiType.StrValue))
                    writer.WriteValue(multiType.StrValue);
                else
                    throw new JsonSerializationException("Invalid value for MultiType");
            }
            else
                throw new JsonSerializationException("Null writer value");
        }
    }

    public class ElementListConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<Element>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var result = new List<Element?>();
            JArray jsonArray = JArray.Load(reader);

            foreach (var item in jsonArray)
                result.Add(item.Type switch
                {
                    JTokenType.String => new Element { ID = item.Value<string>() ?? "" },
                    JTokenType.Object => item.ToObject<Element>(),
                    _ => throw new JsonSerializationException("Invalid Element type")
                });

            return result;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value != null)
            {
                var list = (List<Element>)value;
                writer.WriteStartArray();
                foreach (var item in list) writer.WriteValue(item.ID);
                writer.WriteEndArray();
            }
            else
            {
                throw new JsonSerializationException("Null writer value");
            }
        }
    }

    public class CamSpriteConverter : JsonConverter<CamSprite>
    {
        public override CamSprite ReadJson(JsonReader reader, Type objectType, CamSprite? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                CamSprite sprite = new();
                JArray array = JArray.Load(reader);
                if (array.Count == 3)
                {
                    sprite.Sprite = array[0].Value<string>();
                    sprite.X = array[1].Value<int>();
                    sprite.Y = array[2].Value<int>();
                    return sprite;
                }
                if (array.Count == 4)
                {
                    sprite.Sprite = array[0].Value<string>();
                    sprite.X = array[1].Value<int>();
                    sprite.Y = array[2].Value<int>();
                    sprite.Selected = array[3].Value<string>();
                    if (sprite.Selected == "")
                    {
                        sprite.Selected = sprite.Sprite;
                    }
                    return sprite;
                }
            }
            throw new JsonException("Invalid JSON format for CamButton.");
        }

        public override void WriteJson(JsonWriter writer, CamSprite? value, JsonSerializer serializer)
        {
            Logger.LogErrorAsync("FEJson", "Writing JSON is not supported in this converter.");
        }
    }

    #endregion
}