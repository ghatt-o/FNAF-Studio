using System.Drawing;
using System.Numerics;
using System.Reflection;
using ImGuiNET;

namespace Editor.Controls;

public class PropertiesControl
{
    private object? targetObject;

    public PropertiesControl(object obj)
    {
        SetObj(obj);
    }

    public void SetObj(object obj)
    {
        targetObject = obj ?? throw new ArgumentNullException(nameof(obj));
    }

    public void Render()
    {
        if (targetObject == null)
            throw new InvalidOperationException("Target object is not set.");

        var type = targetObject.GetType();

        if (ImGui.BeginChild("ScrollableProperties", new Vector2(600, 173f), ImGuiChildFlags.None,
                ImGuiWindowFlags.HorizontalScrollbar))
        {
            if (ImGui.BeginTable("PropertiesTable", 2,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.Reorderable |
                    ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Property Name", ImGuiTableColumnFlags.WidthFixed, 200);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 400);
                ImGui.TableHeadersRow();

                ImGui.PushItemWidth(300);
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    if (prop.CanRead && prop.CanWrite)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text(prop.Name);

                        ImGui.TableSetColumnIndex(1);
                        RenderProperty(prop);
                    }

                ImGui.EndTable();
            }

            ImGui.EndChild();
        }
    }

    private void RenderProperty(PropertyInfo property)
    {
        if (targetObject == null) return;
        var value = property.GetValue(targetObject);
        var propertyName = property.Name;

        switch (value)
        {
            case int intValue:
                if (ImGui.InputInt($"##{propertyName}", ref intValue))
                    property.SetValue(targetObject, intValue);
                break;
            case float floatValue:
                if (ImGui.InputFloat($"##{propertyName}", ref floatValue))
                    property.SetValue(targetObject, floatValue);
                break;
            case double doubleValue:
                if (ImGui.InputDouble($"##{propertyName}", ref doubleValue))
                    property.SetValue(targetObject, doubleValue);
                break;
            case bool boolValue:
                if (ImGui.Checkbox($"##{propertyName}", ref boolValue))
                    property.SetValue(targetObject, boolValue);
                break;
            case string stringValue:
                if (ImGui.InputText($"##{propertyName}", ref stringValue, 256))
                    property.SetValue(targetObject, stringValue);
                break;
            case Color colorValue:
                var colorVector = new Vector4(colorValue.R / 255f, colorValue.G / 255f, colorValue.B / 255f,
                    colorValue.A / 255f);
                if (ImGui.ColorEdit4($"##{propertyName}", ref colorVector))
                {
                    colorValue = Color.FromArgb(
                        (int)Math.Clamp(colorVector.W * 255, 0, 255),
                        (int)Math.Clamp(colorVector.X * 255, 0, 255),
                        (int)Math.Clamp(colorVector.Y * 255, 0, 255),
                        (int)Math.Clamp(colorVector.Z * 255, 0, 255)
                    );
                    property.SetValue(targetObject, colorValue);
                }

                break;
            default:
                ImGui.Text($"Unsupported Type ({value?.GetType().Name})");
                break;
        }
    }
}