﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RandomFileCopier.Dialogs;
using RandomFileCopier.Helpers;
using RandomFileCopier.Logic;
using RandomFileCopier.Logic.Helper;
using RandomFileCopier.Models;
using RandomFileCopier.Models.Selection;
using RandomFileCopier.ViewModel.Base;

namespace RandomFileCopier.ViewModel
{
    class VideoCopyViewModel
        : FileCopyViewModel<VideoSourceDestinationModel, VideoSelectionModel , VideoFileRepresenter>
    {
        private readonly IVideoFileRepresenterFactory _fileRepresenterFactory;
        private readonly IRandomVideoFileSelector _randomFileSelector;

        public VideoCopyViewModel(IFileSearcher fileSearcher, IVideoFileRepresenterFactory videoFileRepresenterFactory, IDispatcherWrapper dispatcher, IRandomVideoFileSelector randomFileSelector, ISerializationHelper serializationHelper, IDialogService dialogService, IOpenerHelper openerHelper, IConfigurationHelper configurationHelper)
            : base( fileSearcher ?? new FileSearcher(), dispatcher ?? new DispatcherWrapper(), serializationHelper?? new SerializationHelper(), dialogService ?? new DialogService(), openerHelper ?? new OpenerHelper(), configurationHelper ?? new ConfigurationHelper())
        {
            _fileRepresenterFactory = videoFileRepresenterFactory  ?? new VideoFileRepresenterFactory();
            _randomFileSelector = randomFileSelector ?? new RandomVideoFileSelector();
            SelectionModel = new VideoSelectionModel(0, 10);
            var settings = ConfigurationHelper.GetExtensions(ExtensionsAppsettingKey.VideoExtensions);
            Extensions = new ObservableCollection<string>(settings.Select(x => x.Extension));
            Model = new VideoSourceDestinationModel(settings.Where(x => x.DefaultSelected).Select(x => x.Extension));
        }

        public VideoCopyViewModel()
            : this(null, null,null,null, null, null, null, null)
        {

        }

        protected override void OnSelectionModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var propertyName = e.PropertyName;
            if (propertyName == nameof(SelectionModel.VideosWithSubtitlesOnly))
            {
                Model.IncludeSubtitles = SelectionModel.VideosWithSubtitlesOnly? true : Model.IncludeSubtitles;
            }
        }


        protected override VideoFileRepresenter CreateFileRepresenter(FileInfo fileInfo)
        {
            return _fileRepresenterFactory.CreateVideoFileRepresenter(fileInfo, Model.IncludeSubtitles);
        }

        protected override Task SelectRandomFilesAsync(IEnumerable<VideoFileRepresenter> copyRepresenter, IEnumerable<CopiedFile> copiedFileList,  CancellationToken token)
        {
            return _randomFileSelector.SelectMaximumAmountOfRandomFilesAsync(copyRepresenter,SelectionModel.MinimumFileSizeInBytes, SelectionModel.MaximumFileSizeInBytes, SelectionModel.SelectedSizeInBytes, SelectionModel.VideosWithSubtitlesOnly, copiedFileList, token );
        }
    }
}
