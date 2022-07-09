using Avalonia.Controls;

using ReactiveUI;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NuGet.Tools.Desktop.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private int _mode;

        internal int Mode
        {
            get { return _mode; }
            set { this.RaiseAndSetIfChanged(ref _mode, value); }
        }

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
            Mode = 0;
            if (string.IsNullOrWhiteSpace(PackageId)) return;

            //var repository = PackageRepositoryFactory.CreateRepository(PackageRepositoryFactory.BaseV2Url);
            //var resource = await repository.GetResourceAsync<FindPackageByIdResource>();
            //using var context = new SourceCacheContext();
            //var rets = await resource.GetAllVersionsAsync(PackageId, context, NullLogger.Instance, CancellationToken.None);

            var vs = await NuGetClient.VersionsAsync(PackageId);
            Versions.Clear();
            SelectedItems.Clear();

            if (vs == null || vs.Length == 0) return;

            foreach (var item in vs)
            {
                var v = new PackageVersion()
                {
                    Id = PackageId,
                    Content = item
                };
                Versions.Add(v);
            }
            Mode = 2;
        }

        public async void OnPushBrowseCommand(Window window)
        {
            Mode = 0;
            var dialog = new OpenFileDialog()
            {
                Title = "Select NuGet Packages",
                AllowMultiple = true,
                Filters = new List<FileDialogFilter>()
                {
                    new FileDialogFilter(){ Extensions=new List<string>{"*.nupkg"}, Name="NuGet Package"},
                }
            };
            var result = await dialog.ShowAsync(window);

            Versions.Clear();
            SelectedItems.Clear();

            if (result != null)
            {
                foreach (var item in result)
                {
                    var v = new PackageVersion()
                    {
                        Content = item
                    };
                    Versions.Add(v);
                    SelectedItems.Add(v);
                }
                Mode = 1;
            }
        }

        public async void OnPushCommand()
        {
            if (Mode != 1 || string.IsNullOrWhiteSpace(NuGetApiKey)) return;

            if (SelectedItems.Count == 0) return;

            //var repository = PackageRepositoryFactory.CreateRepository(PackageRepositoryFactory.BaseUrl);
            //var updateResource = await repository.GetResourceAsync<PackageUpdateResource>();

            var selected = SelectedItems.ToArray();
            foreach (var item in selected)
            {
                if (item == null || item.Content == null) continue;

                //bool ret = true;
                //try
                //{
                //    await updateResource.Push(new[] { item.Content }, null, 999, false, s => NuGetApiKey, s => NuGetApiKey, false, true, null, NullLogger.Instance);
                //}
                //catch (System.Exception)
                //{
                //    ret = false;
                //}

                using var fs = new FileStream(item.Content, FileMode.Open, FileAccess.Read);
                var ret = await NuGetClient.PushAsync(fs, NuGetApiKey);
                Versions.First(x => x.Content == item.Content).Status = ret ? "Pushed" : "Failed";
            }
        }

        public async void OnDeleteCommand()
        {
            if (Mode != 2 || string.IsNullOrWhiteSpace(NuGetApiKey)) return;

            if (SelectedItems.Count == 0) return;

            //var repository = PackageRepositoryFactory.CreateRepository(PackageRepositoryFactory.BaseUrl);
            //var updateResource = await repository.GetResourceAsync<PackageUpdateResource>();

            var selected = SelectedItems.ToArray();
            foreach (var item in selected)
            {
                if (item == null) continue;

                //bool ret = true;
                //try
                //{
                //    await updateResource.Delete(item.Id, item.Content, s => NuGetApiKey, _ => true, false, NullLogger.Instance);
                //}
                //catch (System.Exception)
                //{
                //    ret = false;
                //}

                var ret = await NuGetClient.DeleteAsync(item.Id!, item.Content!, NuGetApiKey);
                Versions.First(x => x.Content == item.Content).Status = ret ? "Deleted" : "Failed";
            }
        }

        public async void OnListCommand()
        {
            if (Mode != 2 || string.IsNullOrWhiteSpace(NuGetApiKey)) return;

            if (SelectedItems.Count == 0) return;

            //var repository = PackageRepositoryFactory.CreateRepository(PackageRepositoryFactory.PublishUrl);
            //var updateResource = await repository.GetResourceAsync<PackageUpdateResource>();

            var selected = SelectedItems.ToArray();
            foreach (var item in selected)
            {
                if (item == null) continue;
                var ret = await NuGetClient.RelistAsync(item.Id!, item.Content!, NuGetApiKey);
                Versions.First(x => x.Content == item.Content).Status = ret ? "Listed" : "Failed";
            }
        }
    }

    public class PackageVersion : ViewModelBase
    {
        private string? _status;
        public string? Id { get; set; }
        public string? Content { get; set; }

        public string? Status
        {
            get { return _status; }
            set { this.RaiseAndSetIfChanged(ref _status, value); }
        }
    }
}