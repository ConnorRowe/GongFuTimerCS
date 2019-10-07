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

        public MainPage()
        {
            this.InitializeComponent();
            teaTimer = new Timer();
            alarmSound = new MediaElement();

            //Init timer so it displays at 0 seconds at the start
            teaTimer.Clear();

            //Load alarmSound from file
            LoadAlarmFile();

            //Get dispatcher for main loop
            Windows.UI.Core.CoreWindow appWindow = Windows.UI.Core.CoreWindow.GetForCurrentThread();
            appDispatcher = appWindow.Dispatcher;

            appWindow.Activated += AppWindow_Activated;

            //------------------------------- Main Loop -------------------------------
            MainLoop();
        }

        //Async
        public async void LoadAlarmFile()
        {
            StorageFolder folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            //folder = await folder.GetFolderAsync("Assets");
            StorageFile sf = await folder.GetFileAsync("Assets\\Alarm.wav");
            Stream st = await sf.OpenStreamForReadAsync();
            alarmSound.AutoPlay = false;
            alarmSound.SetSource(st.AsRandomAccessStream(), sf.ContentType);
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
                    //Wait 10ms if nothing needs to happen
                    else
                    {
                        Task.Delay(10).Wait();
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
            if (isFocused)
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

        //Events

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            teaTimer.Clear();
            infNumber = 0;
            alarmSound.Stop();
            infNumText.Text = "0";
            //Also reset startbutton text
            startButton.Content = "Start";
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
    }
}
