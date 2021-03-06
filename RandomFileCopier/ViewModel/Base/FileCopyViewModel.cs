﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RandomFileCopier.Dialogs;
using RandomFileCopier.Helpers;
using RandomFileCopier.Logic;
using RandomFileCopier.Logic.Helper;
using RandomFileCopier.Models;
using RandomFileCopier.Models.Base;
using RandomFileCopier.Models.Selection.Base;

namespace RandomFileCopier.ViewModel.Base
{
    abstract class FileCopyViewModel<TSourceDestinationModel, TSelectionModel, TCopyRepresenter>
        : CopyViewModel<TSourceDestinationModel, TSelectionModel, TCopyRepresenter>
        where TSourceDestinationModel : SourceDestinationModel<TCopyRepresenter>
        where TCopyRepresenter : CopyRepresenter
        where TSelectionModel : SelectionModel
    {

        public FileCopyViewModel(IFileSearcher fileSearcher, IDispatcherWrapper dispatcher, ISerializationHelper serializationHelper, IDialogService dialogService, IOpenerHelper openerHelper, IConfigurationHelper configurationHelper)
            : base(dispatcher, serializationHelper, dialogService, openerHelper)
        {
            FileSearcher = fileSearcher;
            ConfigurationHelper = configurationHelper;
        }

        protected override async Task SpecificSearchAsync(string path, CancellationToken token)
        {
            var searchOption = Model.IncludeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var item in FileSearcher.SearchFiles(path, Model.SelectedExtensions, searchOption, token))
            {
                var newFile = CreateFileRepresenter(item);
                Dispatcher.Invoke(() => Model.Items.Add(newFile));
            }

            var copiedFileList = GetFileListOrNullIfNotApplicable();

            await SelectRandomFilesAsync(Model.Items, copiedFileList, token);
        }

        protected abstract TCopyRepresenter CreateFileRepresenter(FileInfo fileInfo);

        protected override Task<IListWithErrorDictionary<CopiedFile>> CopySpecificAsync(CancellationToken token)
        {
            return Task.Run<IListWithErrorDictionary<CopiedFile>>(async () => {
                var copiedFileList = new ListWithErrorDictionary<CopiedFile>();
                var selectedFiles = Model.Items.Where(x => x.IsSelected).ToList();
                MaxProgressBarValue = selectedFiles.Count;
                Progress = 0;
                var index = 0;
                while (index < selectedFiles.Count && !token.IsCancellationRequested)
                {
                    Progress++;
                    var file = selectedFiles[index];
                    try
                    {
                        using (var fileStream = File.Open(file.Path, FileMode.Open))
                        {
                            var destinationFilePath = Path.Combine(Model.DestinationPath, file.Name);
                            using (var destinationStream = File.Create(destinationFilePath))
                            {
                                await fileStream.CopyToAsync(destinationStream, 81920, token);
                            }
                        }
                        copiedFileList.Add(new CopiedFile(file.Name, file.Size, DateTime.Now));
                    }
                    catch (Exception exc) when (exc is UnauthorizedAccessException || exc is IOException)
                    {
                        copiedFileList.AddError(file.Path, exc.Message);
                    } //swallow unauthorizedaccessexceptions
                    index++;
                }

                return copiedFileList;
            });
        }

        public IFileSearcher FileSearcher { get; private set; }
        public IConfigurationHelper ConfigurationHelper { get; private set; }
        public ObservableCollection<string> Extensions { get; set; }
    }
}
