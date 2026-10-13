using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AiOPDF;

/// <summary>Live presentation bindings with weak ownership; never translates editable user text.</summary>
public static class LocalizedUI
{
    public static readonly AttachedProperty<bool> EnabledProperty = AvaloniaProperty.RegisterAttached<Control, Control, bool>("LocalizationEnabled", true);
    static readonly ConditionalWeakTable<AvaloniaObject, State> states = new();
    static readonly List<WeakReference<AvaloniaObject>> targets = [];
    static bool installed;
    sealed class State
    {
        public readonly Dictionary<AvaloniaProperty, string> Sources = [];
        public readonly Dictionary<AvaloniaProperty, (string Key, object?[] Args)> Keys = [];
        public bool Updating;
    }
    public static void Install()
    {
        if (installed) return; installed = true;
        TextBlock.TextProperty.Changed.AddClassHandler<TextBlock>((c, _) => Track(c, TextBlock.TextProperty));
        ContentControl.ContentProperty.Changed.AddClassHandler<ContentControl>((c, _) => Track(c, ContentControl.ContentProperty));
        TextBox.PlaceholderTextProperty.Changed.AddClassHandler<TextBox>((c, _) => Track(c, TextBox.PlaceholderTextProperty));
        ComboBox.PlaceholderTextProperty.Changed.AddClassHandler<ComboBox>((c, _) => Track(c, ComboBox.PlaceholderTextProperty));
        Window.TitleProperty.Changed.AddClassHandler<Window>((c, _) => Track(c, Window.TitleProperty));
        AutomationProperties.NameProperty.Changed.AddClassHandler<Control>((c, _) => Track(c, AutomationProperties.NameProperty));
        ToolTip.TipProperty.Changed.AddClassHandler<Control>((c, _) => Track(c, ToolTip.TipProperty));
        L10n.Changed += () => { if (Dispatcher.UIThread.CheckAccess()) Refresh(); else Dispatcher.UIThread.Post(Refresh); };
    }
    static void Track(AvaloniaObject control, AvaloniaProperty property)
    {
        if (control is Control c && !c.GetValue(EnabledProperty)) return;
        var state = states.GetValue(control, c =>
        {
            if (targets.Count >= 4096) targets.RemoveAll(reference => !reference.TryGetTarget(out _));
            targets.Add(new WeakReference<AvaloniaObject>(c)); return new State();
        });
        if (state.Updating) return;
        state.Keys.Remove(property);
        if (control.GetValue(property) is not string source) { state.Sources.Remove(property); return; }
        state.Sources[property] = source;
        Apply(control, property, source, state);
    }
    static void Apply(AvaloniaObject control, AvaloniaProperty property, string source, State state)
    {
        state.Updating = true;
        try { control.SetValue(property, L10n.Translate(source)); }
        finally { state.Updating = false; }
    }
    /// <summary>Use stable resource IDs for new UI. Call again when the message arguments change.</summary>
    public static void Bind(AvaloniaObject control, AvaloniaProperty property, string key, params object?[] args)
    {
        var state = states.GetValue(control, c => { targets.Add(new WeakReference<AvaloniaObject>(c)); return new State(); });
        state.Sources.Remove(property); state.Keys[property] = (key, args);
        ApplyKey(control, property, key, args, state);
    }
    static void ApplyKey(AvaloniaObject control, AvaloniaProperty property, string key, object?[] args, State state)
    {
        state.Updating = true;
        try { control.SetValue(property, args.Length == 0 ? L10n.Text(key) : L10n.Format(key, args)); }
        finally { state.Updating = false; }
    }
    static void Refresh()
    {
        for (var i = targets.Count - 1; i >= 0; i--)
        {
            if (!targets[i].TryGetTarget(out var control)) { targets.RemoveAt(i); continue; }
            if (control is Control c && !c.GetValue(EnabledProperty)) continue;
            if (states.TryGetValue(control, out var state))
            {
                foreach (var binding in state.Sources.ToArray()) Apply(control, binding.Key, binding.Value, state);
                foreach (var binding in state.Keys.ToArray()) ApplyKey(control, binding.Key, binding.Value.Key, binding.Value.Args, state);
            }
        }
    }
}
