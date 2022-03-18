using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TreeViewTest.Infrastructure;
using TreeViewTest.ValueObjects;


namespace TreeViewTest.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _startDictionary = string.Empty;
    private string _regexString = string.Empty;
    private string _currentDictionary = string.Empty;
    private FindAndAll _findAndAll;
    private TimeSpan _timeElapsed;

    private readonly Stopwatch _stopwatch = new();
    private DelegateCommand _startAndPauseCommand;
    private CancellationTokenSource _cancellationTokenSource = new();
    private string _startAndPauseButtonContent = "Start";

    public MainWindowViewModel()
    {
        _startAndPauseCommand = new DelegateCommand(Start);
    }

    #region Properties
    
    public string StartDictionary
    {
        get => _startDictionary;
        set
        {
            _startDictionary = value;
            OnPropertyChanged();
        }
    }

    public string RegexString
    {
        get => _regexString;
        set
        {
            _regexString = value;
            OnPropertyChanged();
        }
    }

    public string CurrentDictionary
    {
        get => _currentDictionary;
        set
        {
            _currentDictionary = value;
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

    public DelegateCommand SelectDictionaryCommand { get; set; }

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
        StartAndPauseButtonContent = "Pause";
        StartAndPauseCommand = new DelegateCommand(Pause);
        StartTimer(_cancellationTokenSource.Token);
    }

    private async void Pause()
    {
        _stopwatch.Stop();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        StartAndPauseCommand = new DelegateCommand(Start);
        StartAndPauseButtonContent = "Continue";
    }

    private async Task StartTimer(CancellationToken cancellationToken = default)
    {
        _stopwatch.Start();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TimeElapsed = _stopwatch.Elapsed;
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }
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