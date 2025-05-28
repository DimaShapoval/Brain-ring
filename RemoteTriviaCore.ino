#include <WiFi.h>
#include <WiFiUdp.h>
#include <cstring>  // Для strlen()

#define NETWORK_SSID "ControlPoint"
#define NETWORK_PASS "pass123456"

const uint16_t COMMUNICATION_PORT = 9090;

// Опис кнопок команд
struct ButtonUnit {
  uint8_t pin;             // GPIO-пін
  const char* identifier;  // Ідентифікатор повідомлення
};

// Налаштування пінів для 4-х кнопок
ButtonUnit teamButtons[] = {
  {14, "unit_one"},
  {12, "unit_two"},
  {13, "unit_three"},
  {15, "unit_four"}
};

const uint8_t ledIndicatorPin = 2;             // Пін для LED-індикатора
WiFiUDP messageInterface;                      // UDP-інтерфейс
const IPAddress broadcastIP(192,168,4,255);     // Широкомовна адреса AP

// Налаштовуємо ESP32 як Wi-Fi точку доступу
void initializeNetwork() {
  WiFi.softAP(NETWORK_SSID, NETWORK_PASS);
  delay(500);  // чекаємо підняття AP
}

// Налаштовуємо піні: кнопки — як входи, LED — як вихід
void configureInputs() {
  for (auto& btn : teamButtons) {
    pinMode(btn.pin, INPUT_PULLUP);
  }
  pinMode(ledIndicatorPin, OUTPUT);
  digitalWrite(ledIndicatorPin, LOW);
}

// Надсилаємо UDP-повідомлення на broadcast IP
void dispatchSignal(const char* payload) {
  messageInterface.beginPacket(broadcastIP, COMMUNICATION_PORT);
  messageInterface.write((const uint8_t*)payload, strlen(payload));
  messageInterface.endPacket();
}

// Пошук першої натиснутої кнопки
int findPressedButton() {
  for (int i = 0; i < sizeof(teamButtons)/sizeof(teamButtons[0]); ++i) {
    // При INPUT_PULLUP кнопка замикає на GND → читаємо LOW
    if (digitalRead(teamButtons[i].pin) == LOW) {
      return i;
    }
  }
  return -1;
}

// Основний цикл: обробка натискань з дебаунсом
void pollingCycle() {
  static unsigned long lastTransmission = 0;
  static bool blocked = false;

  int idx = findPressedButton();
  if (idx != -1 && !blocked && (millis() - lastTransmission > 300)) {
    // Відправляємо ідентифікатор команди
    dispatchSignal(teamButtons[idx].identifier);
    // Для індикації можна мигнути LED
    digitalWrite(ledIndicatorPin, HIGH);
    delay(50);
    digitalWrite(ledIndicatorPin, LOW);

    lastTransmission = millis();
    blocked = true;
  }
  if (idx == -1) {
    blocked = false;
  }
}

void setup() {
  Serial.begin(115200);
  configureInputs();
  initializeNetwork();
}

void loop() {
  pollingCycle();
}
