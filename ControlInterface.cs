using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RemoteTriviaCore
{
    /// <summary>
    /// Головне вікно для керування ігровою сесією.
    /// </summary>
    public partial class ControlInterface : Window
    {
        private readonly NetworkHandlerUnit _receiver;
        private readonly RoundSessionCore _session;

        private readonly DispatcherTimer _mainTimer;
        private readonly DispatcherTimer _bonusTimer;

        private int _timeCounter;
        private int _questionIndex;


        public ControlInterface()
        {
            InitializeComponent();

            _receiver = new NetworkHandlerUnit();
            _session = new RoundSessionCore(2); // тимчасово задано 2 команди

            _mainTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _bonusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            _mainTimer.Tick += MainTimer_Tick;
            _bonusTimer.Tick += BonusTimer_Tick;

            _receiver.MessageReceived += HandleIncomingMessage;
            _receiver.StartReceiving();

            InitInterface();
        }

        private void InitInterface()
        {
            countdownDisplay.Content = "0";
            questionDisplay.Content = "1";
            _questionIndex = 1;
        }

        private void MainTimer_Tick(object sender, EventArgs e)
        {
            _timeCounter++;
            countdownDisplay.Content = _timeCounter.ToString();

            if (_timeCounter == 50 || _timeCounter >= 60)
            {
                PlaySound("Sounds/EndTime.mp3");
            }

            if (_timeCounter >= 60)
            {
                _mainTimer.Stop();
            }
        }

        private void BonusTimer_Tick(object sender, EventArgs e)
        {
            _timeCounter++;
            countdownDisplay.Content = _timeCounter.ToString();

            if (_timeCounter >= 20)
            {
                _bonusTimer.Stop();
                PlaySound("Sounds/EndTime.mp3");
            }
        }

        private void HandleIncomingMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                switch (message)
                {
                    case "start":
                        StartRound();
                        break;

                    case "reset":
                        ResetGame();
                        break;

                    case "newgame":
                        NewGame();
                        break;
                }
            });
        }

        private void StartRound()
        {
            _timeCounter = 0;
            PlaySound("Sounds/Gong.mp3");
            _mainTimer.Start();
            startButton.IsEnabled = false;
        }

        private void ResetGame()
        {
            _mainTimer.Stop();
            _bonusTimer.Stop();
            _timeCounter = 0;
            countdownDisplay.Content = "0";
            startButton.IsEnabled = true;
        }

        private void NewGame()
        {
            ResetGame();
            string selected = teamCountSelector.Text;

            int count = selected switch
            {
                "Две" => 2,
                "Три" => 3,
                "Четыре" => 4,
                _ => 2 // значення за замовчуванням
            };
            CheckingCoutOfTeams(count);
            _questionIndex = 1;
            questionDisplay.Content = _questionIndex.ToString();

        }

        private void PlaySound(string path)
        {
            var player = new MediaPlayer();
            player.Open(new Uri(path, UriKind.Relative));
            player.Play();
        }

        protected override void OnClosed(EventArgs e)
        {
            _mainTimer.Stop();
            _bonusTimer.Stop();
            _receiver.StopReceiving();
            base.OnClosed(e);
        }

        private void CloseAppRoutine(object sender, System.ComponentModel.CancelEventArgs e)
        {
            OnClosed(e);
        }

        private void ResetSessionTrigger(object sender, RoutedEventArgs e)
        {
            ResetGame();
        }

        private void LaunchNewGameRoutine(object sender, RoutedEventArgs e)
        {
            NewGame();
        }

        private void StartRoundCommand(object sender, RoutedEventArgs e)
        {
            StartRound();
        }

        private void CheckingCoutOfTeams(int number)
        {
            var scoreLabels = new[] { scoreLabelTeamOne, scoreLabelTeamTwo, scoreLabelTeamThree, scoreLabelTeamFour };
            var indicators = new[] { indicatorImageTeamOne, indicatorImageTeamTwo, indicatorImageTeamThree, indicatorImageTeamFour };
            var teamLabels = new[] { teamLabelOne, teamLabelTwo, teamLabelThree, teamLabelFour };
            var nameInputs = new[] { teamNameInputOne, teamNameInputTwo, teamNameInputThree, teamNameInputFour };
            var inputTeamLabel = new[] { inputTeamLabelOne, inputTeamLabelTwo, inputTeamLabelThree, inputTeamLabelFour };

            for (int i = 0; i < scoreLabels.Length; i++)
            {
                bool isVisible = i < number;

                scoreLabels[i].Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                scoreLabels[i].Content = "0"; // обнуляємо рахунок
                teamLabels[i].Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                nameInputs[i].Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                inputTeamLabel[i].Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;


                indicators[i].Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;

                if (isVisible)
                {
                    teamLabels[i].Content = nameInputs[i].Text; // оновлюємо назву команди
                }
            }
        }


    }
}