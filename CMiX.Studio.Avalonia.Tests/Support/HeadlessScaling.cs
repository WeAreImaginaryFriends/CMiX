using System.Reflection;
using Avalonia.Controls;

namespace CMiX.Studio.Avalonia.Tests.Support
{
    // Sets the render scaling of a headless window. The headless platform has no setting for it, so this writes the private
    // backing field of the window and raises ScalingChanged.
    internal static class HeadlessScaling
    {
        private const string Field = "<RenderScaling>k__BackingField";

        public static void Set(TopLevel window, double scale)
        {
            var impl = window.PlatformImpl ?? throw new InvalidOperationException("The window has no platform window. Show it first.");
            var type = impl.GetType();
            var field = type.GetField(Field, BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw Changed($"{type.FullName} has no field {Field}");
            var changed = type.GetProperty("ScalingChanged")
                ?? throw Changed($"{type.FullName} has no property ScalingChanged");

            field.SetValue(impl, scale);
            ((Action<double>?)changed.GetValue(impl))?.Invoke(scale);
        }

        private static InvalidOperationException Changed(string what) =>
            new($"{what}. Avalonia {typeof(TopLevel).Assembly.GetName().Version} changed the headless window. Update HeadlessScaling.");
    }
}
