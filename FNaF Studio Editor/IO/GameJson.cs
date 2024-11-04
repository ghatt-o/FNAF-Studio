using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Editor.IO;

public class GameJson
{
    public class Game
    {
        public Dictionary<string, Animatronic> Animatronics { get; set; } = [];
        public Dictionary<string, CamUI> Cameras { get; set; } = [];
        public GameInfo GameInfo { get; set; } = new();
        public Dictionary<string, Menu> Menus { get; set; } = [];
        public Dictionary<string, Office> Offices { get; set; } = [];
        public Office Office { get; set; } = new();
        public Sounds Sounds { get; set; } = new();
        public List<string> Loaded_extensions { get; set; } = [];

        [JsonIgnore] public string Name { get; set; } = string.Empty;
        [JsonIgnore] public Dictionary<string, List<Code>> OfficeScripts { get; set; } = [];

        [JsonIgnore] public Dictionary<string, List<AnimationJson>> Animations { get; set; } = [];

        public void Save()
        {
            if (string.IsNullOrEmpty(Name))
                throw new NoNullAllowedException("Project name is null.");

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters =
                [
                    new ElementListConverter(),
                    new MultiTypeConverter()
                ]
            };

            // Save Game JSON
            var gameJsonPath = AppDomain.CurrentDomain.BaseDirectory + "data/projects/" +
                               Name + "/game.json";
            File.WriteAllText(gameJsonPath, JsonConvert.SerializeObject(this, settings));

            // Save Office Scripts
            var scriptsPath = AppDomain.CurrentDomain.BaseDirectory + "data/projects/" +
                              ProjectManager.projectSpecialNameSelected + "/scripts/";
            Directory.CreateDirectory(scriptsPath);

            foreach (var script in OfficeScripts)
            {
                var scriptFilePath = Path.Combine(scriptsPath, script.Key + ".fescript");
                File.WriteAllText(scriptFilePath, JsonConvert.SerializeObject(script.Value, settings));
            }
        }

        public static Game Load(string inputJsonPath)
        {
            var content = File.ReadAllText(inputJsonPath);
            var serializerSettings = new JsonSerializerSettings
            {
                Converters =
                [
                    new ElementListConverter(),
                    new MultiTypeConverter()
                ]
            };
            var gameJson = JsonConvert.DeserializeObject<Game>(content, serializerSettings);
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
        public bool Phantom { get; set; } = false;
        public bool Script { get; set; } = false; // call a certain event on jumpscare
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
        public Dictionary<string, List<MultiType>> Buttons { get; set; } = [];
        public List<int> MusicBox { get; set; } = [];
        public Dictionary<string, List<MultiType>> Sprites { get; set; } = [];
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
        public List<JToken> Args { get; set; } = [];
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
    }

    public class Office
    {
        public List<OfficeLayer> Layers = [];
        public Animations Animations { get; set; } = new();
        public List<OfficeObject> Objects { get; set; } = [];
        public Power Power { get; set; } = new();
        public Dictionary<string, string> States { get; set; } = [];
        public Dictionary<string, UIButton> UIButtons { get; set; } = [];

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
        public string OnSprite { get; set; } = string.Empty;
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

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            if (reader.Value != null)
                return reader.TokenType switch
                {
                    JsonToken.String => new MultiType((string)reader.Value),
                    JsonToken.Integer => new MultiType(Convert.ToInt32(reader.Value)),
                    _ => throw new JsonSerializationException("Invalid type for MultiType")
                };
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
            {
                throw new JsonSerializationException("Null writer value");
            }
        }
    }

    public class ElementListConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<Element>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            var result = new List<Element?>();
            var jsonArray = JArray.Load(reader);

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

    #endregion
}