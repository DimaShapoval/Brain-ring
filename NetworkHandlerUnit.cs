using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteTriviaCore
{
    /// <summary>
    /// Клас для отримання повідомлень через UDP протокол.
    /// </summary>
    public class NetworkHandlerUnit
    {
        /// <summary>
        /// Делегат для обробки отриманих повідомлень.
        /// </summary>
        /// <param name="message">Текст отриманого повідомлення.</param>
        public delegate void MessageReceivedHandler(string message);

        /// <summary>
        /// Подія, що викликається при надходженні нового повідомлення.
        /// </summary>
        public event MessageReceivedHandler MessageReceived;

        private UdpClient _udpListener;
        private CancellationTokenSource _cts;
        private int _listeningPort;
        private bool _isListening;

        /// <summary>
        /// Ініціалізація слухача на вказаному порту.
        /// </summary>
        /// <param name="port">Порт для прийому повідомлень.</param>
        public NetworkHandlerUnit(int port = 9090)
        {
            _listeningPort = port;
        }

        /// <summary>
        /// Почати прийом повідомлень.
        /// </summary>
        public void StartReceiving()
        {
            if (_isListening)
                return;

            _udpListener = new UdpClient(_listeningPort);
            _cts = new CancellationTokenSource();
            _isListening = true;

            Task.Run(() => ListenLoop(_cts.Token));
        }

        /// <summary>
        /// Зупинити прийом повідомлень.
        /// </summary>
        public void StopReceiving()
        {
            if (!_isListening)
                return;

            _cts.Cancel();
            _udpListener.Close();
            _isListening = false;
        }

        /// <summary>
        /// Основний цикл прийому повідомлень.
        /// </summary>
        /// <param name="token">Токен скасування операції.</param>
        private async Task ListenLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var result = await _udpListener.ReceiveAsync();

                    string receivedText = Encoding.UTF8.GetString(result.Buffer);

                    MessageReceived?.Invoke(receivedText);
                }
            }
            catch (ObjectDisposedException)
            {
                // Сокет був закритий під час завершення — нормальна ситуація
            }
            catch (Exception ex)
            {
                // Виведення помилки при роботі з мережею
                Console.WriteLine($"Помилка отримання UDP повідомлення: {ex.Message}");
            }
        }
    }
}
