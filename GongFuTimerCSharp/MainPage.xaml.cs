using Microsoft.Toolkit.Uwp.UI.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GongFuTimerCSharp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public bool isFocused = false;
        public Windows.UI.Core.CoreDispatcher appDispatcher;

        //Timing vars
        public Timer teaTimer;
        public long ticks;
        public int infNumber = 0;
        public float targetSeconds = 0.0f;
        public DateTime lastFrameTime;
        public double delta;

        //Alarm sound
        public MediaElement alarmSound;

        //Debug
        String debugText = "";

        //Tea presets
        PresetCollection presets = new PresetCollection();

        public MainPage()
        {
            this.InitializeComponent();
            teaTimer = new Timer();
            alarmSound = new MediaElement();

            //Init timer so it displays at 0 seconds at the start
            teaTimer.Clear();

            //Load alarmSound from file
            LoadAlarmFile();

            LoadPresetsFromFile();

            //Get dispatcher for main loop
            Windows.UI.Core.CoreWindow appWindow = Windows.UI.Core.CoreWindow.GetForCurrentThread();
            appDispatcher = appWindow.Dispatcher;

            appWindow.Activated += AppWindow_Activated;
            this.Loaded += MainPage_Loaded;

            //------------------------------- Main Loop -------------------------------
            MainLoop();
        }

        //Enumerator
        public enum AppSection
        {
            Timer,
            Presets,
            Settings
        }

        //Async

        public async void LoadAlarmFile()
        {
            StorageFolder folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFile sf = await folder.GetFileAsync("Assets\\Alarm.wav");
            Stream st = await sf.OpenStreamForReadAsync();
            alarmSound.AutoPlay = false;
            alarmSound.SetSource(st.AsRandomAccessStream(), sf.ContentType);
        }

        public async void LoadPresetsFromFile()
        {
            //Get the file path in appdata/local
            StorageFile file = null;
            String path = System.IO.Directory.CreateDirectory(ApplicationData.Current.LocalFolder.Path + "\\GongFuTimer\\").ToString();
            StorageFolder jFolder = await StorageFolder.GetFolderFromPathAsync(path);

            //check if it exists or not
            if (System.IO.File.Exists(path + "presets.json"))
            {
                file = await jFolder.GetFileAsync("presets.json");
            }
            else
            {
                file = await jFolder.CreateFileAsync("presets.json");
            }

            //Read from the file
            String json = await FileIO.ReadTextAsync(file);

            //Convert from JSON to PresetCollection
            presets = JsonConvert.DeserializeObject<PresetCollection>(json);

            //validate presets, setting it if the file was completely blank
            if (presets == null)
            {
                presets = new PresetCollection();
            }

            presetDataGrid.ItemsSource = presets.Presets;
        }

        public async void SavePresetsToFile()
        {
            StorageFile file;
            String path = System.IO.Directory.CreateDirectory(ApplicationData.Current.LocalFolder.Path + "\\GongFuTimer\\").ToString();
            StorageFolder jFolder = await StorageFolder.GetFolderFromPathAsync(path);

            if (System.IO.File.Exists(path + "presets.json"))
            {
                file = await jFolder.GetFileAsync("presets.json");
            }
            else
            {
                file = await jFolder.CreateFileAsync("presets.json");
            }

            String json = JsonConvert.SerializeObject(presets, Formatting.Indented);

            //Write the PresetCollection data to the file
            await FileIO.WriteTextAsync(file, json);
        }

        public async void MainLoop()
        {
            await appDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                while(true)
                {
                    //Increment ticks
                    ++ticks;

                    //calculate frame delta to prevent more than 60 frames from occuring every second
                    TimeSpan timeDelta = DateTime.Now - lastFrameTime;
                    delta = timeDelta.TotalMilliseconds;

                    if(delta >= 1000.0 / 60.0)
                    {
                        //Run update function
                        Update();

                        //Reset last frame time
                        lastFrameTime = DateTime.Now;
                    }
                    //Process any UI events if needed
                    else if (appDispatcher.ShouldYield())
                    {
                        appDispatcher.ProcessEvents(Windows.UI.Core.CoreProcessEventsOption.ProcessAllIfPresent);
                    }
                    //Wait 4ms if nothing needs to happen
                    else
                    {
                        Task.Delay(4).Wait();
                    }
                }
            });
        }

        //Helper functions

        public float StoF(String str)
        {
            return float.Parse(str);
        }

        public String FormatFloat(float f)
        {
            String formattedStr;

            if(f < 10.0f)
            {
                formattedStr = "0" + f.ToString();
            }
            else
            {
                formattedStr = f.ToString();
            }

            return formattedStr;
        }

        //Main functions

        public void Update()
        {
            //Only check for input if the window is focussed
            if (isFocused && GongFuGrid.Visibility == Visibility.Visible)
            {
                //Check for enter pressed
                if (Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Enter).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                {
                    Start();
                }
            }

            //Check if timer is complete
            if (teaTimer.isRunning && teaTimer.ElapsedSeconds() > targetSeconds)
            {
                //---- Timer has fired!

                teaTimer.Clear();
                alarmSound.Play();
                //Increment number of infusions
                ++infNumber;
                infNumText.Text = infNumber.ToString();
                //Change startbutton content
                startButton.Content = "Next Infusion";
                //Clear timer text
                minuteText.Text = "00";
                secondText.Text = "00";
                millisecondText.Text = "00";
            }

            //Splitting the second value from the timer into minutes, seconds, and milliseconds for the timer display
            float seconds = 0.0f;
            float minutes = 0.0f;
            float milliseconds = 0.0f;

            //Only update text UI stuff if window is focused
            if (isFocused)
            {
                if (teaTimer.isRunning)
                {
                    seconds = (float)(targetSeconds - teaTimer.ElapsedSeconds());
                    if (seconds >= 60.0f)
                    {
                        minutes = seconds / 60.0f;
                        minutes = (float)Math.Floor(minutes);
                        seconds -= minutes * 60.0f;
                    }
                    milliseconds = (seconds - (float)Math.Floor(seconds)) * 100.0f;
                    seconds = (float)Math.Floor(seconds);
                    milliseconds = (float)Math.Floor(milliseconds);

                    minuteText.Text = FormatFloat(minutes);
                    secondText.Text = FormatFloat(seconds);
                    millisecondText.Text = FormatFloat(milliseconds);
                }

                debugTextBlock.Text = debugText;
            }
        }

        public void Start()
        {
            //target Seconds = base time + (additional infusion time * infusion number)
            targetSeconds = StoF(baseSecsTextBox.Text.ToString()) + (StoF(infSecsTextBox.Text.ToString()) * (float)infNumber);

            if(targetSeconds > 0.0f)
                teaTimer.Start();
        }

        public void SwitchDisplay(AppSection section)
        {
            //get colours
            SolidColorBrush white = new SolidColorBrush(Windows.UI.Colors.White);
            SolidColorBrush highlight = (SolidColorBrush)Application.Current.Resources["Highlight"];

            //Reset colours
            TimerMenu.Foreground = white;
            LoadPresetMenu.Foreground = white;
            SettingsMenu.Foreground = white;

            switch (section)
            {
                case AppSection.Timer:
                    GongFuGrid.Visibility = Visibility.Visible;
                    PresetGrid.Visibility = Visibility.Collapsed;
                    TimerMenu.Foreground = highlight;
                    break;
                case AppSection.Settings:
                    break;
                case AppSection.Presets:
                    GongFuGrid.Visibility = Visibility.Collapsed;
                    PresetGrid.Visibility = Visibility.Visible;
                    LoadPresetMenu.Foreground = highlight;
                    break;
            }
        }

        public void ApplyTea(Tea tea)
        {
            baseSecsTextBox.Text = tea.BaseSeconds.ToString();
            infSecsTextBox.Text = tea.PlusSeconds.ToString();
            ResetTimer();
        }

        public void ResetTimer()
        {
            teaTimer.Clear();
            infNumber = 0;
            alarmSound.Stop();
            infNumText.Text = "0";
            //Also reset startbutton text
            startButton.Content = "Start";
        }

        //Events

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetTimer();
        }

        private void AppWindow_Activated(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                isFocused = false;
            }
            else
            {
                isFocused = true;
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //Find the tea type combobox column and set its enum type
            foreach (DataGridColumn column in presetDataGrid.Columns)
            {
                if (column.Tag.ToString() == "Type")
                {
                    (column as DataGridComboBoxColumn).ItemsSource = Enum.GetValues(typeof(TeaType)).Cast<TeaType>();
                }
            }
        }

        private void TimerMenu_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SwitchDisplay(AppSection.Timer);
        }

        private void PresetMenu_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SwitchDisplay(AppSection.Presets);
        }

        private void LoadPreset_Click(object sender, RoutedEventArgs e)
        {
            if (presetDataGrid.SelectedItem != null)
            {
                ApplyTea((Tea)presetDataGrid.SelectedItem);
                SwitchDisplay(AppSection.Timer);
            }
        }

        //DataGrid sorting stuff
        private void presetDataGrid_Sort(object sender, DataGridColumnEventArgs e)
        {
            if(e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Ascending)
            {
                //Use the Tag property to pass the bound column name for the sorting implementation
                if(e.Column.Tag.ToString() == "Name")
                {

                    //Implement ascending sort on the column "Range" using LINQ
                    presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(from preset in presets.Presets
                                                                        orderby preset.Name descending
                                                                        select preset);
                }
                if (e.Column.Tag.ToString() == "Type")
                {
                    presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(from preset in presets.Presets orderby preset.Type descending select preset);
                }
                if (e.Column.Tag.ToString() == "BaseSeconds")
                {
                    presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(from preset in presets.Presets orderby preset.BaseSeconds descending select preset);
                }
                if (e.Column.Tag.ToString() == "PlusSeconds")
                {
                    presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(from preset in presets.Presets orderby preset.PlusSeconds descending select preset);
                }
                if (e.Column.Tag.ToString() == "Temp")
                {
                    presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(from preset in presets.Presets orderby preset.Temp descending select preset);
                }
                e.Column.SortDirection = DataGridSortDirection.Descending;

                if (e.Column.Tag.ToString() == "AltName")
                {
                    e.Column.SortDirection = null;
                }

                //Reset all other columns
                foreach(DataGridColumn column in (sender as DataGrid).Columns)
                {
                    if (column.Tag != e.Column.Tag)
                        column.SortDirection = null;
                }
            }
            else if (e.Column.SortDirection == DataGridSortDirection.Descending)
            {
                if (e.Column.Tag.ToString() == "Name")
                {
                    presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(from preset in presets.Presets orderby preset.Name ascending select preset);
                }
                if (e.Column.Tag.ToString() == "Type")
                {
                    presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(from preset in presets.Presets orderby preset.Type ascending select preset);
                }
                if (e.Column.Tag.ToString() == "BaseSeconds")
                {
                    presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(from preset in presets.Presets orderby preset.BaseSeconds ascending select preset);
                }
                if (e.Column.Tag.ToString() == "PlusSeconds")
                {
                    presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(from preset in presets.Presets orderby preset.PlusSeconds ascending select preset);
                }
                if (e.Column.Tag.ToString() == "Temp")
                {
                    presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(from preset in presets.Presets orderby preset.Temp ascending select preset);
                }
                e.Column.SortDirection = DataGridSortDirection.Ascending;
            }
        }

        //Adding a new tea preset
        private void NewPresetButton_Click(object sender, RoutedEventArgs e)
        {
            presets.Presets.Add(new Tea("New", "", 0, 0, 0, 0, 0));
            presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(presets.Presets);
        }

        private void SavePresetButton_Click(object sender, RoutedEventArgs e)
        {
            SavePresetsToFile();
        }

        //Delete the selected tea preset
        private void DeletePresetButton_Click(object sender, RoutedEventArgs e)
        {
            presets.Presets.Remove((Tea)presetDataGrid.SelectedItem);
            presetDataGrid.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Tea>(presets.Presets);
        }
    }

    //Tea

    public enum TeaType
    {
        White,
        Green,
        Matcha,
        Yellow,
        Oolong,
        Black,
        RawPuerh,
        Ripened,
        Tisanes,
        MedicinalHerbs
    };

    public class Tea
    {
        public String Name { get; set; }
        public String AltName { get; set; }
        public TeaType Type { get; set; }
        public ushort BaseSeconds { get; set; }
        public ushort PlusSeconds { get; set; }
        public ushort Temp { get; set; }
        public ushort MaxInfusions { get; set; }

        public Tea(String name, String altname, TeaType type, ushort baseseconds, ushort plusseconds, ushort temp, ushort maxinfusions)
        {
            this.Name = name;
            this.AltName = altname;
            this.Type = type;
            this.BaseSeconds = baseseconds;
            this.PlusSeconds = plusseconds;
            this.Temp = temp;
            this.MaxInfusions = maxinfusions;
        }

        public static PresetCollection GetTestTeas()
        {
            return new PresetCollection(new List<Tea>(new Tea[2] {
                new Tea("Souchong Liquour", "Tong Mu Zhengshan Xiaozhong", TeaType.Black, 15, 5, 90, 8),
                new Tea("Silver Needle", "Bai Hao Yin Zhen", TeaType.White, 45, 10, 90, 5)
            }));
        }

    }

    public class PresetCollection
    {
        public List<Tea> Presets { get; set; }

        public PresetCollection(List<Tea> presets)
        {
            Presets = presets;
        }

        public PresetCollection()
        {
            Presets = new List<Tea>();
        }
    }
}
