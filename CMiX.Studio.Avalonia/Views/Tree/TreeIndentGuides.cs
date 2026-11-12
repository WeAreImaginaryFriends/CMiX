// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CMiX.Studio.Avalonia.ViewModels.Tree;

namespace CMiX.Studio.Avalonia.Views
{
    // The indent guides of a row of the tree: a line of GuideWidth in the middle of each indent step before the chevron of the row.
    // The line of a step belongs to the row above at that level. It has ActiveBrush when that row is on the branch of the cursor
    // (TreeNode.IsOnCursorBranch), else Brush. The control asks for no room and takes no click, so the row does not move.
    // It draws the lines itself and has no children, so a row that leaves the tree changes no visual tree.
    public sealed class TreeIndentGuides : Control
    {
        public static readonly StyledProperty<int> LevelProperty =
            AvaloniaProperty.Register<TreeIndentGuides, int>(nameof(Level));

        public static readonly StyledProperty<double> IndentProperty =
            AvaloniaProperty.Register<TreeIndentGuides, double>(nameof(Indent));

        public static readonly StyledProperty<double> GuideWidthProperty =
            AvaloniaProperty.Register<TreeIndentGuides, double>(nameof(GuideWidth), 1);

        public static readonly StyledProperty<IBrush?> BrushProperty =
            AvaloniaProperty.Register<TreeIndentGuides, IBrush?>(nameof(Brush));

        public static readonly StyledProperty<IBrush?> ActiveBrushProperty =
            AvaloniaProperty.Register<TreeIndentGuides, IBrush?>(nameof(ActiveBrush));

        static TreeIndentGuides()
        {
            AffectsRender<TreeIndentGuides>(LevelProperty, IndentProperty, GuideWidthProperty, BrushProperty, ActiveBrushProperty);
            IsHitTestVisibleProperty.OverrideDefaultValue<TreeIndentGuides>(false);
        }

        public int Level
        {
            get => GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
        }

        public double Indent
        {
            get => GetValue(IndentProperty);
            set => SetValue(IndentProperty, value);
        }

        public double GuideWidth
        {
            get => GetValue(GuideWidthProperty);
            set => SetValue(GuideWidthProperty, value);
        }

        public IBrush? Brush
        {
            get => GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        public IBrush? ActiveBrush
        {
            get => GetValue(ActiveBrushProperty);
            set => SetValue(ActiveBrushProperty, value);
        }

        // The rows above the row, from the top level down: the owner of the line of each step.
        private readonly List<TreeNode> owners = new();

        // The number of lines: one for each level of the row.
        public int GuideCount => Math.Min(Level, owners.Count);

        // The line of step i, in the middle of the step, on a whole pixel.
        public Rect GuideRect(int i) => new(i * Indent + Math.Floor(Indent / 2), 0, GuideWidth, Bounds.Height);

        public IBrush? GuideBrush(int i) => owners[i].IsOnCursorBranch ? ActiveBrush : Brush;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == LevelProperty || change.Property == DataContextProperty)
                Watch();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Watch();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            Unwatch();
        }

        // Finds the owners of the lines and watches their state.
        private void Watch()
        {
            Unwatch();
            if (DataContext is TreeNode node && VisualRoot != null)
            {
                for (var parent = node.Parent; parent != null && owners.Count < Level; parent = parent.Parent)
                    owners.Insert(0, parent);
                foreach (var owner in owners)
                    owner.PropertyChanged += OnOwnerChanged;
            }

            InvalidateVisual();
        }

        private void Unwatch()
        {
            foreach (var owner in owners)
                owner.PropertyChanged -= OnOwnerChanged;
            owners.Clear();
        }

        private void OnOwnerChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TreeNode.IsOnCursorBranch))
                InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize) => default;

        public override void Render(DrawingContext context)
        {
            for (var i = 0; i < GuideCount; i++)
            {
                if (GuideBrush(i) is { } brush)
                    context.FillRectangle(brush, GuideRect(i));
            }
        }
    }
}
