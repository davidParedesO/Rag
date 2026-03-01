using Rag.ViewModels;
using System.Collections.Specialized;
using System.Linq;

namespace Rag.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        await _viewModel.LoadWorkspacesAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            ScrollToBottom();
        }
    }

    private async void ScrollToBottom()
    {
        var lastMessage = _viewModel.Messages.LastOrDefault();
        if (lastMessage != null)
        {
            // Pequeño retraso para dar tiempo a que la interfaz renderice el nuevo elemento
            await Task.Delay(100);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    MessagesCollectionView.ScrollTo(lastMessage, position: ScrollToPosition.End, animate: false);
                }
                catch
                {
                    // Ignorar posibles errores si la vista se está destruyendo
                }
            });
        }
    }
}
