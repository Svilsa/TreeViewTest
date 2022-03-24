using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using TreeViewTest.Core;
using TreeViewTest.Infrastructure;
using TreeViewTest.Models;
using TreeViewTest.ValueObjects;


namespace TreeViewTest.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string? _startDirectoryPath;
    private string _currentSearchDirectory = string.Empty;
    private Regex _regex = new(".*");
    private ObservableCollection<INode> _startDirectoryMembers = new();
    private IEnumerator<string>? _enumerator;

    private DelegateCommand _startAndPauseCommand;

    private readonly Stopwatch _stopwatch = new();
    private FindAndAll _findAndAll;
    private TimeSpan _timeElapsed;
    private string _startAndPauseButtonContent = "Start";

    private bool _isPaused;
    private CancellationTokenSource _cancellationTokenSource = new();
    private Settings? _settings;
    private readonly SynchronizationContext? _context = SynchronizationContext.Current;
    
    public MainWindowViewModel()
    {
        _startAndPauseCommand = new DelegateCommand(Start);
        CloseWindowCommand = new DelegateCommand(OnCloseWindow);
        
        LoadSettings();
    }

    #region Properties

    public ObservableCollection<INode> StartDirectoryMembers
    {
        get => _startDirectoryMembers;
        set
        {
            _startDirectoryMembers = value;
            OnPropertyChanged();
        }
    }

    public string? StartDirectoryPath
    {
        get => _startDirectoryPath;
        set
        {
            _startDirectoryPath = value;
            _enumerator = null;
            OnPropertyChanged();
        }
    }

    public string? RegexString
    {
        get => _regex.ToString();
        set
        {
            if (value == null) return;
            _enumerator = null;
            _regex = new Regex(value);
            OnPropertyChanged();
        }
    }

    public string CurrentSearchDirectory
    {
        get => _currentSearchDirectory;
        set
        {
            _currentSearchDirectory = value;
            OnPropertyChanged();
        }
    }

    public FindAndAll FindAndAll
    {
        get => _findAndAll;
        set
        {
            _findAndAll = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan TimeElapsed
    {
        get => _timeElapsed;
        set
        {
            _timeElapsed = value;
            OnPropertyChanged();
        }
    }

    public string StartAndPauseButtonContent
    {
        get => _startAndPauseButtonContent;
        set
        {
            _startAndPauseButtonContent = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Commands

    public DelegateCommand CloseWindowCommand { get; set; }

    public DelegateCommand StartAndPauseCommand
    {
        get => _startAndPauseCommand;
        set
        {
            _startAndPauseCommand = value;
            OnPropertyChanged();
        }
    }

    public DelegateCommand StopCommand { get; set; }

    #endregion

    private async void Start()
    {
        try
        {
            switch (_isPaused)
            {
                case false:
                {
                    StartAndPauseButtonContent = "Pause";
                    StartAndPauseCommand = new DelegateCommand(Pause);

                    _stopwatch.Reset();
                    CurrentSearchDirectory = string.Empty;
                    StartDirectoryMembers = new ObservableCollection<INode>();
                    FindAndAll = new FindAndAll();

                    StartTimerAsync(_cancellationTokenSource.Token);
                    goto case true;
                }
                case true:
                {
                    await Task.Run(() => StartOrContinueSearchingFiles(_cancellationTokenSource.Token));
                    break;
                }
                    
            }
            
            StartAndPauseButtonContent = "Start";
            StartAndPauseCommand = new DelegateCommand(Start);
            _isPaused = false;
            
            _enumerator?.Dispose();
            _enumerator = null;
        }
        catch (OperationCanceledException)
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message);
        }
    }

    private void Pause()
    {
        _stopwatch.Stop();
        _cancellationTokenSource.Cancel();
        
        _isPaused = true;
        StartAndPauseCommand = new DelegateCommand(Start);
        StartAndPauseButtonContent = "Continue";
        
    }

    private async Task StartTimerAsync(CancellationToken cancellationToken = default)
    {
        _stopwatch.Start();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TimeElapsed = _stopwatch.Elapsed;
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }
    }

    private void StartOrContinueSearchingFiles(CancellationToken cancellationToken = default)
    {
        _enumerator ??= Directory.EnumerateFiles(StartDirectoryPath, "*",
                new EnumerationOptions { RecurseSubdirectories = true })
            .GetEnumerator();

        var uiCallback = new SendOrPostCallback(state =>
        {
            var (collection, node) = (Tuple<ICollection<INode>, INode>)state!;
            collection.Add(node);
        });

        while (_enumerator.MoveNext())
        {
            CurrentSearchDirectory = _enumerator.Current;
            FindAndAll = new FindAndAll(FindAndAll.Find, FindAndAll.All + 1);

            var pathFromStart = Path.GetRelativePath(StartDirectoryPath, _enumerator.Current);
            var splitPath = pathFromStart.Split("\\");

            if (!_regex.IsMatch(splitPath[^1]))
                continue;

            FindAndAll = new FindAndAll(FindAndAll.Find + 1, FindAndAll.All);
            var currDirectoryMembers = StartDirectoryMembers;


            for (int i = 0; i < splitPath.Length - 1; i++)
            {
                var currDirectory =
                    currDirectoryMembers.FirstOrDefault(node => node.Name == splitPath[i] && node is DirectoryModel);

                if (currDirectory == null)
                {
                    var newDirectory = new DirectoryModel(splitPath[i]);

                    if (_context == null)
                        uiCallback.Invoke(new Tuple<ICollection<INode>, INode>(currDirectoryMembers, newDirectory));
                    else
                        _context.Send(uiCallback,
                            new Tuple<ICollection<INode>, INode>(currDirectoryMembers, newDirectory));

                    currDirectoryMembers = newDirectory.Members;
                }
                else
                {
                    currDirectoryMembers = ((DirectoryModel)currDirectory).Members;
                }
            }

            var newFile = new FileModel(splitPath[^1]);

            if (_context == null)
                uiCallback.Invoke(new Tuple<ICollection<INode>, INode>(currDirectoryMembers, newFile));
            else
                _context.Send(uiCallback, new Tuple<ICollection<INode>, INode>(currDirectoryMembers, newFile));
            
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private void OnCloseWindow()
    {
        _settings ??= new Settings();
        _settings.Path = StartDirectoryPath;
        _settings.Regex = RegexString;
        _settings.TrySave();
    }

    private void LoadSettings()
    {
        Settings.TryLoad(out _settings);
        if (_settings == null)
            return;

        StartDirectoryPath = _settings.Path;
        RegexString = _settings.Regex;
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}