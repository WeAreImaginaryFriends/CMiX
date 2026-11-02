using CMiX.Core.Compositing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // ApplySnapshot applies the model of a saved project to a project.
    public class ProjectApplySnapshotTests
    {
        private static Project NewProject() => TestServiceProviderFactory.Create().GetRequiredService<Project>();

        private static Project ProjectWithThreeCompositions()
        {
            var project = NewProject();
            foreach (var name in new[] { "A", "B", "C" })
            {
                project.CompositionManager.AddItem(typeof(Composition));
                ((Composition)project.CompositionManager.SelectedItem).PrefabService.Name.Value = name;
            }
            return project;
        }

        private static ProjectModel Save(Project project) => (ProjectModel)project.ToModel();

        private static List<string> Names(Project project) =>
            project.CompositionManager.ManagerData.Items.Cast<Composition>().Select(c => c.PrefabService.Name.Value).ToList();

        [Fact]
        public void ApplySnapshot_AddsTheCompositionsInFileOrder()
        {
            var model = Save(ProjectWithThreeCompositions());
            var target = NewProject();

            target.ApplySnapshot(model);

            Assert.Equal(new List<string> { "A", "B", "C" }, Names(target));
        }

        [Fact]
        public void ApplySnapshot_SelectsTheCompositionThatWasSelectedInTheFile()
        {
            var source = ProjectWithThreeCompositions();
            source.CompositionManager.ManagerData.SelectAt(1);
            var model = Save(source);
            var target = NewProject();

            target.ApplySnapshot(model);

            Assert.Equal(1, target.CompositionManager.ManagerData.SelectedIndex);
            Assert.Same(target.CompositionManager.ManagerData.Items[1], target.CompositionManager.SelectedItem);
        }

        [Fact]
        public void ApplySnapshot_WhenTheFileHasNoSelection_LeavesNothingSelected()
        {
            var source = ProjectWithThreeCompositions();
            source.CompositionManager.ManagerData.ClearSelection();
            var model = Save(source);
            var target = NewProject();

            target.ApplySnapshot(model);

            Assert.Equal(-1, target.CompositionManager.ManagerData.SelectedIndex);
        }

        [Fact]
        public void ApplySnapshot_WhenTheSavedIndexIsOutsideTheList_LeavesNothingSelected()
        {
            var model = Save(ProjectWithThreeCompositions());
            model.CompositionManager.ManagerData.SelectedIndex = 7;
            var target = NewProject();

            target.ApplySnapshot(model);

            Assert.Equal(-1, target.CompositionManager.ManagerData.SelectedIndex);
        }

        [Fact]
        public void ApplySnapshot_RestoresTheMasterBeatAndTheNameOfTheFile()
        {
            var source = NewProject();
            source.MasterBeat.Period.Value = 437.5f;
            source.PrefabService.Name.Value = "Saved project";
            var model = Save(source);
            var target = NewProject();

            target.ApplySnapshot(model);

            Assert.Equal(437.5f, target.MasterBeat.Period.Value);
            Assert.Equal("Saved project", target.PrefabService.Name.Value);
        }

        [Fact]
        public void ApplySnapshot_OfAProjectWithoutCompositions_LeavesNothingSelected()
        {
            var model = Save(NewProject());
            var target = NewProject();

            target.ApplySnapshot(model);

            Assert.Empty(target.CompositionManager.ManagerData.Items);
            Assert.Null(target.CompositionManager.SelectedItem);
        }
    }
}
