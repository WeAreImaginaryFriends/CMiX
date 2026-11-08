using System.Collections.ObjectModel;
using CMiX.Studio.Avalonia.ViewModels.Tree;

// In the global namespace, so every test file sees the extensions without a using.
// The tree has one row under its root, the Compositions group. The tests ask for the rows of the compositions under it.
internal static class ProjectTreeTestExtensions
{
    public static TreeNode CompositionsNode(this ProjectTree tree) => tree.Roots[0];

    public static ObservableCollection<TreeNode> CompositionNodes(this ProjectTree tree) => tree.Roots[0].Children;
}
