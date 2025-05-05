#include <ESP8266WiFi.h>
#include <WiFiUdp.h>

#define NETWORK_SSID "ControlPoint"     // Назва точки доступу Wi-Fi
#define NETWORK_PASS "pass123456"       // Пароль для точки доступу

const uint16_t COMMUNICATION_PORT = 8080; // Порт для обміну повідомленнями через UDP

// Структура для опису кнопок команд
struct ButtonUnit {
  uint8_t pin;             // Пін, до якого підключено кнопку
  const char* identifier;  // Ідентифікатор команди
};

// Масив доступних кнопок команд
ButtonUnit teamButtons[] = {
  {14, "unit_one"},
  {12, "unit_two"},
  {13, "unit_three"},
  {15, "unit_four"}
};

const uint8_t ledIndicatorPin = 2; // Світлодіод для візуального контролю стану

WiFiUDP messageInterface;                 // UDP-інтерфейс для надсилання повідомлень
struct station_info* connectedStations;   // Інформація про підключені пристрої
struct ip_addr* stationAddress;           // IP-адреса поточного пристрою

// Ініціалізація Wi-Fi як точки доступу
void initializeNetwork() {
  WiFi.disconnect(); // Вимикаємо будь-які попередні з'єднання
  delay(500);
  WiFi.softAP(NETWORK_SSID, NETWORK_PASS); // Створення точки доступу
}

// Налаштування режиму пінів кнопок та індикатора
void configureInputs() {
  for (auto& button : teamButtons) {
    pinMode(button.pin, INPUT); // Усі кнопки — у режимі INPUT
  }
  pinMode(ledIndicatorPin, OUTPUT);        // Індикатор — вихід
  digitalWrite(ledIndicatorPin, LOW);      // Початковий стан — вимкнений
}

// Надсилання повідомлення UDP-клієнтам
void dispatchSignal(const char* payload) {
  connectedStations = wifi_softap_get_station_info(); // Отримання списку клієнтів

  while (connectedStations != NULL) {
    stationAddress = &connectedStations->ip;
    IPAddress targetIP(stationAddress->addr);

    // Надсилання повідомлення на IP кожного клієнта
    messageInterface.beginPacket(targetIP, COMMUNICATION_PORT);
    messageInterface.write(payload);
    messageInterface.endPacket();

    connectedStations = STAILQ_NEXT(connectedStations, next);
  }
}

// Визначення, яка кнопка була натиснута
int findPressedButton() {
  for (int index = 0; index < sizeof(teamButtons) / sizeof(teamButtons[0]); ++index) {
    if (digitalRead(teamButtons[index].pin) == HIGH) {
      return index;
    }
  }
  return -1; // Якщо жодна кнопка не натиснута
}

// Основна логіка опрацювання натискань із блокуванням повтору
void pollingCycle() {
  static unsigned long lastTransmission = 0;
  static bool transmissionBlocked = false;

  int pressedButtonIndex = findPressedButton();

  if (pressedButtonIndex != -1 && !transmissionBlocked && (millis() - lastTransmission > 300)) {
    dispatchSignal(teamButtons[pressedButtonIndex].identifier); // Надсилання повідомлення
    lastTransmission = millis();     // Запис часу останньої події
    transmissionBlocked = true;      // Заборона повторного спрацювання
  }

  if (pressedButtonIndex == -1) {
    transmissionBlocked = false;     // Розблокування, коли кнопка відпущена
  }
}

// Початкова ініціалізація системи
void prepareEnvironment() {
  Serial.begin(115200);       // Увімкнення серіального монітора
  configureInputs();          // Налаштування пінів
  initializeNetwork();        // Запуск точки доступу
}

// Вхідна точка програми Arduino
void setup() {
  prepareEnvironment();       // Підготовка до роботи
}

// Основний цикл програми
void loop() {
  pollingCycle();             // Безперервне опитування стану кнопок
}
