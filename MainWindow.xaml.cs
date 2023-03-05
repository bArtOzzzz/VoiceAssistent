using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Globalization;
using System.Threading;
using System.Windows;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Controls;
using System.Reflection;
using System.Configuration;

namespace VoiceAssistent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CultureInfo cultureInfo;
        private readonly SpeechRecognitionEngine speechRecognitionEngine;
        private readonly SpeechSynthesizer speechSynthesizer;

        private readonly SpeechRecognitionEngine speechRecognitionEngineParallel;
        private CancellationTokenSource cancellationToken = default!;

        public Random rnd;
        public AssistentResponces assistentResponces;

        public bool isCanBeCanceled = false;
        public bool isSwithedOnAssistentVoice = true;

        private int RecTimeOut = 0;

        public MainWindow()
        {
            cultureInfo = new CultureInfo("en-US");
            speechRecognitionEngine = new SpeechRecognitionEngine(cultureInfo);
            speechSynthesizer = new SpeechSynthesizer();

            speechRecognitionEngineParallel = new SpeechRecognitionEngine(cultureInfo);
            rnd = new Random();
            assistentResponces = new AssistentResponces();

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

        private async void VoiceAssistent_Loaded(object sender, RoutedEventArgs e)
        {
            if (!cultureInfo.Name.Equals(CultureInfo.CurrentCulture.Name))
                ListBox_DialogueStoryLogger.Items.Add($"For the assistent to work correctly, define the system language as English (en). Current language is: {CultureInfo.CurrentCulture}");

            speechRecognitionEngine.SetInputToDefaultAudioDevice();
            speechRecognitionEngine.LoadGrammarAsync(new Grammar(new GrammarBuilder(new Choices(await File.ReadAllLinesAsync("DefaultCommands.txt")))));

            speechRecognitionEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(DefaultCommands_SpeechRecognized);
            speechRecognitionEngine.SpeechDetected += new EventHandler<SpeechDetectedEventArgs>(Recognizer_SpeechRecognized);
            speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);

            speechRecognitionEngineParallel.SetInputToDefaultAudioDevice();
            speechRecognitionEngineParallel.LoadGrammarAsync(new Grammar(new GrammarBuilder(new Choices(File.ReadAllLines(@"DefaultCommands.txt")))));
            speechRecognitionEngineParallel.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Startlistening_SpeechRecognized);

            // Текущее время суток
            ChangeGreeting();


           /* 
            t.Get("IsAutoRun");

            // Автозагрузка сохранений
            CheckBox_isAutorun.DataBindings.Add("Checked", Settings.Default, "IsAutoRun", true, DataSourceUpdateMode.OnPropertyChanged);
            TextBox_ShotDownTimer.DataBindings.Add("Text", Settings.Default, "ShotDownTimer", true, DataSourceUpdateMode.OnPropertyChanged);
            TextBox_HibernateTimer.DataBindings.Add("Text", Settings.Default, "HibernateStateTimer", true, DataSourceUpdateMode.OnPropertyChanged);*/

            // Стартовое системное сообщение
            ListBox_DialogueStoryLogger.Items.Add("System: To view all commands, say Ori \"Show commands\"");
        }

        /// <summary>
        /// Обновление таймера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Recognizer_SpeechRecognized(object sender, SpeechDetectedEventArgs e)
        {
            RecTimeOut = 0;
        }

        /// <summary>
        /// Голосовой ответ ИИ
        /// </summary>
        /// <param name="e"></param>
        /// <param name="index"></param>
        private async void ChooseAnswer(SpeechRecognizedEventArgs e, List<string> index)
        {
            int ranNum;
            string speech = e.Result.Text;

            ranNum = rnd.Next(index.Count);

            if (isSwithedOnAssistentVoice)
            {
                speechSynthesizer.SpeakAsync(index[ranNum]);
            }
            ListBox_DialogueStoryLogger.Items.Add($"You: {speech}");

            await Task.Delay(1000);

            if (index[ranNum] != null)
            {
                ListBox_DialogueStoryLogger.Items.Add($"Ori: {index[ranNum]}");
            }
        }

        private void DefaultCommands_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string socialCommands = e.Result.Text;
            string functionalCommands = e.Result.Text;

            switch (socialCommands)
            {
                case "Hello":
                    ChooseAnswer(e, assistentResponces.Hello_Response);
                    break;
                case "How are you":
                    ChooseAnswer(e, assistentResponces.HowAreYou_Response);
                    break;
                case "Thank you":
                    ChooseAnswer(e, assistentResponces.ThankYou_Response);
                    break;
            }

            switch (functionalCommands)
            {
                case "What time is it":
                    ChooseAnswer(e, assistentResponces.TimeQuery_Response);
                    break;
                case "Stop talking":
                    speechSynthesizer.SpeakAsyncCancelAll();
                    ChooseAnswer(e, assistentResponces.StopTalking_Response);
                    break;
                case "Stop listening":
                    speechRecognitionEngine.RecognizeAsyncCancel();
                    speechRecognitionEngineParallel.RecognizeAsync(RecognizeMode.Multiple);
                    ChooseAnswer(e, assistentResponces.StopListening_Response);
                    break;
                case "Show commands":
                    ListBox_DialogueStoryLogger.Items.Add($"You: {socialCommands}");
                    ShowCommands();
                    break;
                case "Hide commands":
                    ListBox_CommandsList.Visibility = Visibility.Hidden;
                    ListBox_DialogueStoryLogger.Items.Add($"You: {socialCommands}");
                    break;
                case "Shut down the computer":
                    ListBox_DialogueStoryLogger.Items.Add($"You: {socialCommands}");
                    isCanBeCanceled = true;
                    ShutDownTheComputer(e);
                    break;
                case "Cancel":
                    if (isCanBeCanceled)
                    {
                        cancellationToken.Cancel();
                        isCanBeCanceled = false;
                        ChooseAnswer(e, assistentResponces.Cancell_Response);
                    }
                    else
                        ChooseAnswer(e, assistentResponces.ExceptionsLogo_Response);
                    break;
                case "Open settings":
                    Settings_Click(sender, e);
                    ChooseAnswer(e, assistentResponces.OK_Response);
                    break;
                case "Close settings":
                    Settings_Click(sender, e);
                    ChooseAnswer(e, assistentResponces.OK_Response);
                    break;
                case "Check start with windows":
                    AutoRunCheckBoxState();
                    ChooseAnswer(e, assistentResponces.OK_Response);
                    break;
                case "Uncheck start with windows":
                    AutoRunUncheckBoxState();
                    ChooseAnswer(e, assistentResponces.OK_Response);
                    break;
                case "Switch off voice":
                    speechSynthesizer.SpeakAsync("Voice off");
                    isSwithedOnAssistentVoice = false;
                    ListBox_DialogueStoryLogger.Items.Add($"You: {functionalCommands}");
                    break;
                case "Switch on voice":
                    if (isSwithedOnAssistentVoice == false)
                    {
                        speechSynthesizer.SpeakAsync("Voice on");
                        isSwithedOnAssistentVoice = true;
                        ListBox_DialogueStoryLogger.Items.Add($"You: {functionalCommands}");
                    }
                    else
                        ChooseAnswer(e, assistentResponces.ExceptionsLogo_Response);
                    break;
                case "Put the computer to sleep":
                    ListBox_DialogueStoryLogger.Items.Add($"You: {socialCommands}");
                    HibernateState(e);
                    isCanBeCanceled = true;
                    break;
            }
        }

        /// <summary>
        /// Слово - активация ассистента
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Startlistening_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string speech = e.Result.Text;

            if (speech == "Ori" || speech == "Oi")
            {
                speechRecognitionEngineParallel.RecognizeAsyncCancel();
                ChooseAnswer(e, assistentResponces.ActivationAssistentWord_Response);
                speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        /// <summary>
        /// Изменение приветствия в зависимости от времени суток
        /// </summary>
        private void ChangeGreeting()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            if (currentTime >= TimeSpan.Parse("12:01") &&
                currentTime <= TimeSpan.Parse("16:00"))
            {
                Label_GreetingText.Content = "Good afternoon";
            }
            else if (currentTime >= TimeSpan.Parse("16:01") &&
                     currentTime <= TimeSpan.Parse("23:00"))
            {
                Label_GreetingText.Content = "Good evening";
            }
            else if (currentTime >= TimeSpan.Parse("23:01") &&
                     currentTime <= TimeSpan.Parse("4:00"))
            {
                Label_GreetingText.Content = "Good night";
            }
            else if (currentTime >= TimeSpan.Parse("4:01") &&
                     currentTime <= TimeSpan.Parse("12:00"))
            {
                Label_GreetingText.Content = "Good mornin";
            }
        }

        /// <summary>
        /// Таймер до выключения компьютера
        /// </summary>
        /// <param name="token"></param>
        private void ShutDownTimer(CancellationToken token)
        {
            for (int i = Settings.Default.ShotDownTimer; i > 0; i--)
            {
                speechSynthesizer.SpeakAsync(i.ToString());

                Thread.Sleep(1000);

                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (i == 1)
                {
                    Process.Start("cmd", "/c shutdown -s -f -t 00");
                }
            }
        }

        /// <summary>
        /// Выбор состояния для автозапуска программы с Windows
        /// </summary>
        /// <param name="autorun"></param>
        /// <returns></returns>
        public bool SetAutorunValue(bool autorun)
        {
            const string name = "Ori assistent";
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
        /// Метод раскрытия списка команд
        /// </summary>
        private void ShowCommands()
        {
            string[] commands = File.ReadAllLines(@"DefaultCommands.txt");
            ListBox_CommandsList.Items.Clear();
            ListBox_CommandsList.SelectionMode = SelectionMode.Multiple;
            ListBox_CommandsList.Visibility = Visibility.Visible;

            foreach (string command in commands)
            {
                ListBox_CommandsList.Items.Add(command);
            }

            if (GroupBox_SettingsPanel.Visibility.Equals(Visibility.Visible))
                GroupBox_SettingsPanel.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Голосовое выключение компьютера
        /// </summary>
        /// <param name="e"></param>
        private async void ShutDownTheComputer(SpeechRecognizedEventArgs e)
        {
            string speech = e.Result.Text;

            cancellationToken = new CancellationTokenSource();
            var token = cancellationToken.Token;

            try
            {
                await Task.Run(() => ShutDownTimer(token));
            }
            catch (OperationCanceledException)
            {
                ChooseAnswer(e, assistentResponces.ExceptionsLogo_Response);
            }
            finally
            {
                cancellationToken.Dispose();
            }
        }

        /// <summary>
        /// Голосовая ативация автозапуска програмы с Windows
        /// </summary>
        private void AutoRunCheckBoxState()
        {
            CheckBox_isAutorun.IsChecked = true;
            SetAutorunValue(true);
            Settings.Default.IsAutoRun = true;
        }

        /// <summary>
        /// Голосовая отмена автозапуска програмы с Windows
        /// </summary>
        private void AutoRunUncheckBoxState()
        {
            CheckBox_isAutorun.IsChecked = false;
            SetAutorunValue(false);
            Settings.Default.IsAutoRun = false;
        }

        /// <summary>
        /// Голосовая команда перевода компьютера в сон
        /// </summary>
        /// <param name="e"></param>
        private async void HibernateState(SpeechRecognizedEventArgs e)
        {
            string speech = e.Result.Text;

            cancellationToken = new CancellationTokenSource();
            var token = cancellationToken.Token;

            try
            {
                await Task.Run(() => HibernateStateTimer(token));
            }
            catch (OperationCanceledException)
            {
                ChooseAnswer(e, assistentResponces.ExceptionsLogo_Response);
            }
            finally
            {
                cancellationToken.Dispose();
            }
        }

        /// <summary>
        /// Таймер до перевода в сон
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
