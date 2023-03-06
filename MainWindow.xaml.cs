using System.Collections.Generic;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using System.Windows;
using System.IO;
using System;

namespace VoiceAssistant
{
    // TODO: Use async
    public partial class MainWindow : Window
    {
        private readonly SpeechRecognitionEngine speechRecognitionEngine;
        private readonly SpeechSynthesizer speechSynthesizer;
        private readonly CultureInfo cultureInfo;

        private CancellationTokenSource cancellationToken = default!;

        public AssistantResponces assistantResponces;

        private readonly GrammarBuilder grammarBuilder;
        private readonly Grammar grammar;
        private readonly Choices choices;

        public bool isCanBeCanceled = false;
        public bool isSwithedOnAssistantVoice = true;

        private int RecTimeOut = 0;

        public MainWindow()
        {
            cultureInfo = new CultureInfo("en-US");
            speechRecognitionEngine = new SpeechRecognitionEngine(cultureInfo);
            speechSynthesizer = new SpeechSynthesizer();

            // TODO: Change reading from file to Configuration file
            choices = new Choices(File.ReadAllLines("DefaultCommands.txt"));

            grammarBuilder = new GrammarBuilder(choices)
            {
                Culture = cultureInfo
            };
            grammar = new Grammar(grammarBuilder);

            assistantResponces = new AssistantResponces();

            InitializeComponent();
        }

        /// <summary>
        /// Interactivity with <see cref="Label_SettingsText"/>. Makes settings panel visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Click(object sender, EventArgs e)
        {
            if (!GroupBox_SettingsPanel.IsVisible)
            {
                GroupBox_SettingsPanel.Visibility = Visibility.Visible;

                if (ListBox_CommandsList.Visibility.Equals(Visibility.Visible))
                    ListBox_CommandsList.Visibility = Visibility.Hidden;
            }
            else if(GroupBox_SettingsPanel.IsVisible)
                GroupBox_SettingsPanel.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Initialization for assistant after UI was successfully loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VoiceAssistant_Loaded(object sender, RoutedEventArgs e)
        {
            if (!cultureInfo.Equals(CultureInfo.CurrentCulture))
                ListBox_DialogueStoryLogger.Items.Add($"For the assistant to work correctly, define the system language as English (en). Current language is: {CultureInfo.CurrentCulture}");

            speechRecognitionEngine.SetInputToDefaultAudioDevice();
            speechRecognitionEngine.LoadGrammar(grammar);

            // TODO: create delegates
            speechRecognitionEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(DefaultCommands_SpeechRecognized!);
            speechRecognitionEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Startlistening_SpeechRecognized!);

            speechRecognitionEngine.SpeechDetected += new EventHandler<SpeechDetectedEventArgs>(Recognizer_SpeechRecognized!);

            speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);

            ChangeGreeting();

            // Default start message
            ListBox_DialogueStoryLogger.Items.Add("System: To view all commands, say Ori \"Show commands\"");
        }

        /// <summary>
        /// Update timer. Event for <see cref="SpeechRecognitionEngine" />.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Recognizer_SpeechRecognized(object sender, SpeechDetectedEventArgs e)
        {
            RecTimeOut = 0;
        }

        /// <summary>
        /// Chosses random answer for assistent.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="index"></param>
        // TODO: async void...
        private async void AssistantRandomAnswer(SpeechRecognizedEventArgs e, List<string> index)
        {
            int ranNum;
            Random rnd = new();

            ranNum = rnd.Next(index.Count);

            if (isSwithedOnAssistantVoice)
                speechSynthesizer.SpeakAsync(index[ranNum]);

            ListBox_DialogueStoryLogger.Items.Add($"You: {e.Result.Text}");

            await Task.Delay(1000);

            if (index[ranNum] != null)
                ListBox_DialogueStoryLogger.Items.Add($"Ori: {index[ranNum]}");
        }

        /// <summary>
        /// Assistant answers core.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DefaultCommands_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            switch (e.Result.Text)
            {
                case "Hello":
                    AssistantRandomAnswer(e, assistantResponces.Hello_Response);
                    break;
                case "How are you":
                    AssistantRandomAnswer(e, assistantResponces.HowAreYou_Response);
                    break;
                case "Thank you":
                    AssistantRandomAnswer(e, assistantResponces.ThankYou_Response);
                    break;
                case "What time is it":
                    AssistantRandomAnswer(e, assistantResponces.TimeQuery_Response);
                    break;
                case "Stop talking":
                    speechSynthesizer.SpeakAsyncCancelAll();
                    AssistantRandomAnswer(e, assistantResponces.StopTalking_Response);
                    break;
                case "Stop listening":
                    speechRecognitionEngine.RecognizeAsyncCancel();
                    //speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                    AssistantRandomAnswer(e, assistantResponces.StopListening_Response);
                    break;
                case "Show commands":
                    AssistantRandomAnswer(e, assistantResponces.OK_Response);
                    await CommandsList();
                    break;
                case "Hide commands":
                    AssistantRandomAnswer(e, assistantResponces.OK_Response);
                    await CommandsList();
                    break;
                case "Shut down the computer":
                    isCanBeCanceled = true;
                    AssistantRandomAnswer(e, assistantResponces.OK_Response);
                    ShutDownTheComputer(e);
                    break;
                case "Cancel":
                    if (isCanBeCanceled)
                    {
                        cancellationToken.Cancel();
                        AssistantRandomAnswer(e, assistantResponces.Cancell_Response);
                        isCanBeCanceled = false;
                    }
                    else
                        AssistantRandomAnswer(e, assistantResponces.ExceptionsLogo_Response);
                    break;
                case "Open settings":
                    Settings_Click(sender, e);
                    AssistantRandomAnswer(e, assistantResponces.OK_Response);
                    break;
                case "Close settings":
                    Settings_Click(sender, e);
                    AssistantRandomAnswer(e, assistantResponces.OK_Response);
                    break;
                case "Check start with windows":
                    AutoRunCheckBoxState();
                    AssistantRandomAnswer(e, assistantResponces.OK_Response);
                    break;
                case "Uncheck start with windows":
                    AutoRunUncheckBoxState();
                    AssistantRandomAnswer(e, assistantResponces.OK_Response);
                    break;
                case "Switch off voice":
                    isSwithedOnAssistantVoice = false;
                    speechSynthesizer.SpeakAsync("Voice off");
                    ListBox_DialogueStoryLogger.Items.Add($"You: {e.Result.Text}");
                    break;
                case "Switch on voice":
                    if (!isSwithedOnAssistantVoice)
                    {
                        isSwithedOnAssistantVoice = true;
                        speechSynthesizer.SpeakAsync("Voice on");
                        ListBox_DialogueStoryLogger.Items.Add($"You: {e.Result.Text}");
                    }
                    else
                        AssistantRandomAnswer(e, assistantResponces.ExceptionsLogo_Response);
                    break;
                case "Put the computer to sleep":
                    isCanBeCanceled = true;
                    AssistantRandomAnswer(e, assistantResponces.OK_Response);
                    HibernateState(e);
                    break;
            }
        }

        /// <summary>
        /// Keyword for assistent activation - Ori.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Startlistening_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string speech = e.Result.Text;

            if (speech.Equals("Ori") || speech.Equals("Oi"))
            {
                //speechRecognitionEngine.RecognizeAsyncCancel();
                AssistantRandomAnswer(e, assistantResponces.ActivationAssistantWord_Response);
                speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        /// <summary>
        /// Changes greeting depend on current time.
        /// </summary>
        private void ChangeGreeting()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            if (currentTime >= TimeSpan.Parse("12:01") && currentTime <= TimeSpan.Parse("16:00"))
                Label_GreetingText.Content = "Good afternoon";
            else if (currentTime >= TimeSpan.Parse("16:01") && currentTime <= TimeSpan.Parse("23:00"))
                Label_GreetingText.Content = "Good evening";
            else if (currentTime >= TimeSpan.Parse("23:01") && currentTime <= TimeSpan.Parse("4:00"))
                Label_GreetingText.Content = "Good night";
            else if (currentTime >= TimeSpan.Parse("4:01") && currentTime <= TimeSpan.Parse("12:00"))
                Label_GreetingText.Content = "Good mornin";
        }

        /// <summary>
        /// Time before computer will be off.
        /// </summary>
        /// <param name="token"></param>
        private void ShutDownTimer(CancellationToken token)
        {
            for (int i = Settings.Default.ShotDownTimer; i > 0; i--)
            {
                speechSynthesizer.SpeakAsync(i.ToString());

                if (token.IsCancellationRequested)
                    break;

                Thread.Sleep(1000);

                if (i == 1)
                    Process.Start("cmd", "/c shutdown -s -f -t 00");
            }
        }

        /// <summary>
        /// Sets autorun for application if true.
        /// </summary>
        /// <param name="autorun"></param>
        /// <returns></returns>
        public bool SetAutorunValue(bool autorun)
        {
            const string name = "Ori assistant";
            string ExePath = Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.ToString();
            RegistryKey reg;

            reg = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");
            try
            {
                if (autorun)
                    reg.SetValue(name, ExePath);
                else
                    reg.DeleteValue(name);

                reg.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Shows all existed commands.
        /// </summary>
        private async Task<bool> CommandsList()
        {
            if (GroupBox_SettingsPanel.Visibility.Equals(Visibility.Visible))
                GroupBox_SettingsPanel.Visibility = Visibility.Hidden;

            if (ListBox_CommandsList.Visibility.Equals(Visibility.Hidden))
            {
                string[] commands = await File.ReadAllLinesAsync("DefaultCommands.txt");
                ListBox_CommandsList.Items.Clear();
                ListBox_CommandsList.SelectionMode = SelectionMode.Multiple;

                ListBox_CommandsList.Visibility = Visibility.Visible;

                foreach (string command in commands)
                {
                    ListBox_CommandsList.Items.Add(command);
                }

                return true;
            }
            else
                ListBox_CommandsList.Visibility = Visibility.Hidden;

            return false;
        }

        /// <summary>
        /// Shot down the computer command.
        /// </summary>
        /// <param name="e"></param>
        private async void ShutDownTheComputer(SpeechRecognizedEventArgs e)
        {
            cancellationToken = new CancellationTokenSource();
            var token = cancellationToken.Token;

            try
            {
                await Task.Run(() => ShutDownTimer(token));
            }
            catch (OperationCanceledException)
            {
                AssistantRandomAnswer(e, assistantResponces.ExceptionsLogo_Response);
            }
            finally
            {
                cancellationToken.Dispose();
            }
        }

        /// <summary>
        /// Set autorun the application with Windows command.
        /// </summary>
        private void AutoRunCheckBoxState()
        {
            CheckBox_isAutorun.IsChecked = true;
            SetAutorunValue(true);
            Settings.Default.IsAutoRun = true;
        }

        /// <summary>
        /// Unset autorun the application with Windows command.
        /// </summary>
        private void AutoRunUncheckBoxState()
        {
            CheckBox_isAutorun.IsChecked = false;
            SetAutorunValue(false);
            Settings.Default.IsAutoRun = false;
        }

        /// <summary>
        /// Hibernate command.
        /// </summary>
        /// <param name="e"></param>
        private async void HibernateState(SpeechRecognizedEventArgs e)
        {
            cancellationToken = new CancellationTokenSource();
            var token = cancellationToken.Token;

            try
            {
                await Task.Run(() => HibernateStateTimer(token));
            }
            catch (OperationCanceledException)
            {
                AssistantRandomAnswer(e, assistantResponces.ExceptionsLogo_Response);
            }
            finally
            {
                cancellationToken.Dispose();
            }
        }

        /// <summary>
        /// Timer befor computer will be hibernated.
        /// </summary>
        /// <param name="token"></param>
        private void HibernateStateTimer(CancellationToken token)
        {
            for (int i = Settings.Default.HibernateStateTimer; i > 0; i--)
            {
                speechSynthesizer.SpeakAsync(i.ToString());

                Thread.Sleep(1000);

                if (token.IsCancellationRequested)
                    break;

                if (i == 1)
                    Process.Start("cmd", "/c shutdown -s -f -t 00");
            }
        }
    }
}
