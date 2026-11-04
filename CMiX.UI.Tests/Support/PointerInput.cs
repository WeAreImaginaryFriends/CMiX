using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace CMiX.UI.Tests.Support
{
    internal static class PointerInput
    {
        // The headless mouse does not reach every control, so the pointer events are raised on the target.
        public static void Click(Window window, Control target, MouseButton button, Point? at = null)
        {
            var position = Position(window, target, at);
            var pointer = NewPointer();

            Press(window, target, button, position, pointer);
            Release(window, target, button, position, pointer);
        }

        public static void Press(Window window, Control target, MouseButton button, Point? at = null) =>
            Press(window, target, button, Position(window, target, at), NewPointer());

        public static void Release(Window window, Control target, MouseButton button, Point? at = null) =>
            Release(window, target, button, Position(window, target, at), NewPointer());

        private static void Press(Window window, Control target, MouseButton button, Point position, Pointer pointer)
        {
            var left = button == MouseButton.Left;

            window.MouseMove(position);
            Dispatcher.UIThread.RunJobs();
            target.RaiseEvent(new PointerPressedEventArgs(target, pointer, window, position, 1,
                new PointerPointProperties(left ? RawInputModifiers.LeftMouseButton : RawInputModifiers.RightMouseButton,
                    left ? PointerUpdateKind.LeftButtonPressed : PointerUpdateKind.RightButtonPressed), KeyModifiers.None));
        }

        private static void Release(Window window, Control target, MouseButton button, Point position, Pointer pointer)
        {
            var left = button == MouseButton.Left;

            target.RaiseEvent(new PointerReleasedEventArgs(target, pointer, window, position, 2,
                new PointerPointProperties(RawInputModifiers.None,
                    left ? PointerUpdateKind.LeftButtonReleased : PointerUpdateKind.RightButtonReleased), KeyModifiers.None, button));
            Dispatcher.UIThread.RunJobs();
        }

        private static Point Position(Window window, Control target, Point? at) =>
            at ?? target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;

        private static Pointer NewPointer() => new(Pointer.GetNextFreeId(), PointerType.Mouse, true);
    }
}
