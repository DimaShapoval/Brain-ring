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
        private readonly InfoDisplayPanel _infoDisplayPanel;

        private int _timeCounter;
        private int _questionIndex;

        private int? _firstTeamPressed = null;  // ID команди, яка перша натиснула кнопку

        public ControlInterface()
        {
            InitializeComponent();

            _receiver = new NetworkHandlerUnit();
            _session = new RoundSessionCore(2); // тимчасово задано 2 команди
            _infoDisplayPanel = new InfoDisplayPanel();

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
            questionDisplay.Content = "0";
            _questionIndex = 0;
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
                startButton.IsEnabled = true;
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
                // Обробляємо повідомлення від команди
                if (message.StartsWith("buttonPressed"))
                {
                    int teamId = int.Parse(message.Substring("buttonPressed".Length));
                    HandleTeamButtonPress(teamId);  //Обробляємо натискання кнопки
                }
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
            _questionIndex += 1;
            questionDisplay.Content = _questionIndex;
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
            int count = Int32.Parse(teamCountSelector.Text);

            CheckingCoutOfTeams(count);
            _questionIndex = 0;
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

        // Метод для оновлення індикаторів
        private void UpdateIndicators(int teamId)
        {
            var indicators = new[] { indicatorImageTeamOne, indicatorImageTeamTwo, indicatorImageTeamThree, indicatorImageTeamFour };

            // Спочатку вимикаємо всі індикатори
            foreach (var indicator in indicators)
            {
                indicator.Source = new BitmapImage(new Uri("lamp off.jpg", UriKind.Relative));  // Індикатор вимкнений
            }

            // Включаємо індикатор для команди, яка натиснула кнопку першою
            if (teamId >= 0 && teamId < indicators.Length)
            {
                indicators[teamId].Source = new BitmapImage(new Uri("lamp on.jpg", UriKind.Relative));  // Індикатор ввімкнений
            }
        }

        // Метод для обробки натискання кнопок команд
        private void HandleTeamButtonPress(int teamId)
        {
            if (_firstTeamPressed == null)
            {
                // Якщо перша команда ще не вибрана, встановлюємо її
                _firstTeamPressed = teamId;

                // Оновлюємо індикатори
                UpdateIndicators(teamId);

                // Показуємо ведучому, яка команда натиснула першою
                MessageBox.Show($"Перша команда: {GetTeamNameById(teamId)}", "Перша команда", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Метод для отримання назви команди за ID
        private string GetTeamNameById(int teamId)
        {
            switch (teamId)
            {
                case 0: return teamNameInputOne.Text;
                case 1: return teamNameInputTwo.Text;
                case 2: return teamNameInputThree.Text;
                case 3: return teamNameInputFour.Text;
                default: return "Невідома команда";
            }
        }

        // Обробники подій для кнопок команд
        private void TeamOneButton_Click(object sender, RoutedEventArgs e)
        {
            HandleTeamButtonPress(0);  // 0 - ID першої команди
        }

        private void TeamTwoButton_Click(object sender, RoutedEventArgs e)
        {
            HandleTeamButtonPress(1);  // 1 - ID другої команди
        }

        private void TeamThreeButton_Click(object sender, RoutedEventArgs e)
        {
            HandleTeamButtonPress(2);  // 2 - ID третьої команди
        }

        private void TeamFourButton_Click(object sender, RoutedEventArgs e)
        {
            HandleTeamButtonPress(3);  // 3 - ID четвертої команди
        }


    }
}
