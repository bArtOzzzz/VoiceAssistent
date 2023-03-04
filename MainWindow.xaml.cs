using Microsoft.Speech.Recognition;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VoiceAssistent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SpeechRecognitionEngine _recognizer = new SpeechRecognitionEngine(new CultureInfo("en-US"));

        public MainWindow()
        {
            InitializeComponent();
        }

        static Label l;

        static void speechCommands(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence > 0.82) l.Content = e.Result.Text;
        }

        private void VoiceAssistent_Loaded(object sender, RoutedEventArgs e)
        {
            l = Label1;

            SpeechRecognitionEngine speechRecognitionEngine = new SpeechRecognitionEngine();
            speechRecognitionEngine.SetInputToDefaultAudioDevice();

            speechRecognitionEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(speechCommands);

            Choices colors = new Choices();
            colors.Add(new string[] { "one", "two", "three" });

            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(colors);

            Grammar g = new Grammar(gb);
            speechRecognitionEngine.LoadGrammar(g);

            speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
        }



        private void Settings_Click(object sender, MouseButtonEventArgs e)
        {
            if(!GroupBox_SettingsPanel.IsVisible)
            {
                GroupBox_SettingsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                GroupBox_SettingsPanel.Visibility = Visibility.Hidden;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }
    }
}
