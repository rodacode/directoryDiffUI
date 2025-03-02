namespace DirectoryDiffUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ComparisonViewModel Comparison { get; } = new ComparisonViewModel();
    }
}