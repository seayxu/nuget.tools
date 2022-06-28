using ReactiveUI;

using System.Collections.ObjectModel;
using System.Linq;

namespace NuGet.Tools.Desktop.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string? PackageId { get; set; }
        public string? NuGetApiKey { get; set; }
        public ObservableCollection<PackageVersion> Versions { get; set; }
        public ObservableCollection<PackageVersion> SelectedItems { get; set; }

        public MainWindowViewModel()
        {
            Versions = new ObservableCollection<PackageVersion>();
            SelectedItems = new ObservableCollection<PackageVersion>();
        }

        public async void OnGetCommand()
        {
            if (string.IsNullOrWhiteSpace(PackageId)) return;
            var vs = await NuGetClient.VersionsAsync(PackageId);
            Versions.Clear();
            SelectedItems.Clear();

            if (vs == null || vs.Length == 0) return;

            foreach (var item in vs)
            {
                var v = new PackageVersion()
                {
                    Id = PackageId,
                    Checked = false,
                    Version = item
                };
                Versions.Add(v);
            }
        }

        public async void OnDeleteCommand()
        {
            if (string.IsNullOrWhiteSpace(NuGetApiKey)) return;

            if (SelectedItems.Count == 0) return;
            var selected = SelectedItems.ToArray();
            foreach (var item in selected)
            {
                if (item == null) continue;
                var ret = await NuGetClient.DeleteAsync(item.Id!, item.Version!, NuGetApiKey);
                Versions.First(x => x.Version == item.Version).Status = ret ? "Deleted" : "Failed";
                //Versions.Remove(Versions.First(x => x.Version == item.Version));
            }
        }

        public async void OnListCommand()
        {
            if (string.IsNullOrWhiteSpace(NuGetApiKey)) return;

            if (SelectedItems.Count == 0) return;
            var selected = SelectedItems.ToArray();
            foreach (var item in selected)
            {
                if (item == null) continue;
                var ret = await NuGetClient.RelistAsync(item.Id!, item.Version!, NuGetApiKey);
                Versions.First(x => x.Version == item.Version).Status = ret ? "Listed" : "Failed";
                //Versions.Remove(Versions.First(x => x.Version == item.Version));
            }
        }
    }

    public class PackageVersion : ViewModelBase
    {
        private string? _status;
        public bool Checked { get; set; }
        public string? Id { get; set; }
        public string? Version { get; set; }

        public string? Status
        {
            get { return _status; }
            set { this.RaiseAndSetIfChanged(ref _status, value); }
        }
    }
}