using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace DonorSelector9000
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<Donor> Donors = new ObservableCollection<Donor>();
        public ObservableCollection<Donor> DonorsShowing = new ObservableCollection<Donor>();
        public ObservableCollection<Donor> Winners = new ObservableCollection<Donor>();

        public Donor SelectedDonor
        {
            get
            {
                return _selectedDonor;
            }
            set
            {
                if (_selectedDonor == value)
                {
                    return;
                }

                _selectedDonor = value;

                OnPropertyChanged();
            }
        }
        private Donor _selectedDonor;

        public string DialogDonorName
        {
            get
            {
                return _dialogDonorName;
            }
            set
            {
                _dialogDonorName = value;

                OnPropertyChanged();
            }
        }
        private string _dialogDonorName;

        public string DialogDonorAmount
        {
            get
            {
                return _dialogDonorAmount;
            }
            set
            {
                _dialogDonorAmount = value;

                OnPropertyChanged();
            }
        }
        private string _dialogDonorAmount = "0";

        public Donor LastWinner
        {
            get
            {
                return _lastWinner;
            }
            set
            {
                _lastWinner = value;

                OnPropertyChanged();
            }
        }
        private Donor _lastWinner;

        public string SearchText
        {
            get
            {
                return _searchText;
            }
            set
            {
                _searchText = value;

                UpdateDonorsShowing();

                OnPropertyChanged();
            }
        }
        private string _searchText = "";

        public string SortButtonText
        {
            get
            {
                return _sortButtonText;
            }
            set
            {
                _sortButtonText = value;

                UpdateDonorsShowing();

                OnPropertyChanged();
            }
        }
        private string _sortButtonText = "Sort By Amount";

        private bool SortByName
        {
            get
            {
                return _sortByName;
            }
            set
            {
                _sortByName = value;

                SortButtonText = value ? "Sort By Amount" : "Sort By Name";

                UpdateDonorsShowing();
            }
        }
        private bool _sortByName = true;

        public MainPage()
        {
            this.InitializeComponent();

            Donors.CollectionChanged += DonorsChanged;
        }

        private async void Page_Loading(Windows.UI.Xaml.FrameworkElement sender, object args)
        {
            await LoadAsync();
        }

        private async void AddButtonClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SelectedDonor = null;

            DialogDonorAmount = "0";
            DialogDonorName = "";

            await Dialog.ShowAsync();
        }

        private async void EditButtonClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (SelectedDonor != null)
            {
                DialogDonorAmount = SelectedDonor.Amount.ToString();
                DialogDonorName = SelectedDonor.Name;

                await Dialog.ShowAsync();
            }
        }

        private async void RemoveButtonClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (SelectedDonor != null)
            {
                Donors.Remove(SelectedDonor);
                SelectedDonor = Donors.FirstOrDefault();

                await SaveAsync();
            }
        }

        private async void GenerateWinnerButtonClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!Donors.Any())
            {
                return;
            }

            var donorEntries = new List<Donor>();

            foreach (var donor in Donors)
            {
                int donorEntryCount = donor.Amount / 5;

                for (var index = 0; index < donorEntryCount; index++)
                {
                    donorEntries.Add(donor);
                }
            }

            if(!donorEntries.Any())
            {
                return;
            }

            var random = new Random();

            var winnerIndex = random.Next(0, donorEntries.Count);

            var winner = donorEntries[winnerIndex];

            LastWinner = winner;
            Winners.Add(winner);
            Donors.Remove(winner);

            await SaveAsync();

            await WinnerDialog.ShowAsync();
        }

        private async void ClearCacheButtonClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var dataFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync("data.json") as StorageFile;

            if (dataFile != null)
            {
                await dataFile.DeleteAsync();
            }

            Donors.Clear();
            Winners.Clear();
            LastWinner = null;
        }


        private async void ShowPastWinnersButtonClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await PastWinnersDialog.ShowAsync();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (SelectedDonor is null)
            {
                var newDonor = new Donor
                {
                    Name = DialogDonorName,
                    Amount = Convert.ToInt32(DialogDonorAmount)
                };

                Donors.Insert(0, newDonor);

                SelectedDonor = newDonor;
            }
            else
            {
                var selectedDonor = SelectedDonor;

                SelectedDonor.Name = DialogDonorName;
                SelectedDonor.Amount = Convert.ToInt32(DialogDonorAmount);

                var selectedIndex = DonorsShowing.IndexOf(SelectedDonor);

                Donors.Remove(selectedDonor);
                Donors.Insert(selectedIndex, selectedDonor);

                SelectedDonor = selectedDonor;
            }

            await SaveAsync();
        }

        private void DialogTextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            int value;
            if (!int.TryParse(args.NewText, out value) && !string.IsNullOrWhiteSpace(args.NewText))
            {
                args.Cancel = true;
            }
        }

        private void DialogTextBox_LostFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DialogDonorAmount))
            {
                DialogDonorAmount = "0";
            }
        }

        private async Task SaveAsync()
        {
            var dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("data.json", CreationCollisionOption.ReplaceExisting);

            var data = new Data
            {
                Donors = Donors,
                Winners = Winners
            };

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);

            await FileIO.WriteTextAsync(dataFile, json);
        }

        private async Task LoadAsync()
        {
            var dataFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync("data.json") as StorageFile;

            if (dataFile != null)
            {
                var json = await FileIO.ReadTextAsync(dataFile);

                var data = JsonConvert.DeserializeObject<Data>(json);

                foreach (var donor in data.Donors)
                {
                    Donors.Add(donor);
                }

                foreach (var winner in data.Winners)
                {
                    Winners.Add(winner);
                }
            }
        }

        private void DonorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateDonorsShowing();
        }

        private void UpdateDonorsShowing()
        {
            DonorsShowing.Clear();

            var donors = string.IsNullOrWhiteSpace(SearchText) ? Donors : Donors.Where(x => x.Name.ToLower().Contains(SearchText.ToLower()) ||
                                                                                            x.Amount.ToString().ToLower().Contains(SearchText.ToLower()));

            donors = SortByName ? donors.OrderBy(x => x.Name) : donors.OrderByDescending(x => x.Amount);

            foreach (var donor in donors)
            {
                DonorsShowing.Add(donor);
            }
        }

        private void SortButtonClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SortByName = !SortByName;
        }
    }
}