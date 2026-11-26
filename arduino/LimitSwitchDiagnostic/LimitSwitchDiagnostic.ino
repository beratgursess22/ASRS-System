// Motor-safe diagnostic for RAMPS X- and Z- limit switch inputs.
// This sketch never generates step pulses and keeps all drivers disabled.

constexpr uint8_t X_LIMIT_PIN = 3;
constexpr uint8_t Z_LIMIT_PIN = 18;
constexpr uint8_t X_ENABLE_PIN = 38;
constexpr uint8_t Z_ENABLE_PIN = 30;
constexpr uint8_t Y_ENABLE_PIN = 56;
constexpr uint8_t LIMIT_TRIGGERED = LOW;

int lastXState = -1;
int lastZState = -1;

void printLimitStates() {
  const int xState = digitalRead(X_LIMIT_PIN);
  const int zState = digitalRead(Z_LIMIT_PIN);

  if (xState == lastXState && zState == lastZState) {
    return;
  }

  lastXState = xState;
  lastZState = zState;

  Serial.print(F("LIMIT_STATE X_TRIGGERED="));
  Serial.print(xState == LIMIT_TRIGGERED ? 1 : 0);
  Serial.print(F(" Z_TRIGGERED="));
  Serial.println(zState == LIMIT_TRIGGERED ? 1 : 0);
}

void setup() {
  pinMode(X_ENABLE_PIN, OUTPUT);
  pinMode(Z_ENABLE_PIN, OUTPUT);
  pinMode(Y_ENABLE_PIN, OUTPUT);
  digitalWrite(X_ENABLE_PIN, HIGH);
  digitalWrite(Z_ENABLE_PIN, HIGH);
  digitalWrite(Y_ENABLE_PIN, HIGH);

  pinMode(X_LIMIT_PIN, INPUT_PULLUP);
  pinMode(Z_LIMIT_PIN, INPUT_PULLUP);

  Serial.begin(9600);
  Serial.println(F("LIMIT_DIAGNOSTIC_READY"));
  printLimitStates();
}

void loop() {
  printLimitStates();
  delay(20);
}
