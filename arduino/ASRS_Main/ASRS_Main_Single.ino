/**
 * ASRS_Main_Single.ino
 * Tek dosyalik (single-file) Arduino Mega surumu.
 */

#include <Arduino.h>
#include <string.h>

// =========================
// config.h
// =========================

// RAMPS 1.6 + Mega 2560 standart pin eslestirmesi.
// Bu dosyada X ve Z limit switch'ler sadece X- ve Z- endstop girislerinde kullaniliyor.
// Standart RAMPS Mega haritasi:
// X STEP 54, DIR 55, ENABLE 38, MIN 3, MAX 2
// Y STEP 60, DIR 61, ENABLE 56, MIN 14, MAX 15
// Z STEP 46, DIR 48, ENABLE 62, MIN 18, MAX 19
#define X_STEP_PIN 54
#define X_DIR_PIN 55
#define X_ENABLE_PIN 38
#define X_LIMIT_PIN 3   // RAMPS X- / Mega D3

#define Z_STEP_PIN 36
#define Z_DIR_PIN 34
#define Z_ENABLE_PIN 30
#define Z_LIMIT_PIN 18  // RAMPS Z- / Mega D18

#define Y_STEP_PIN 60
#define Y_DIR_PIN 61
#define Y_ENABLE_PIN 56

#define STEPS_PER_REV 200
#define MICROSTEP_FACTOR 32
#define PULLEY_TEETH 20
#define BELT_PITCH_MM 2.0f
#define STEPS_PER_MM 160.0f

#define SPEED_X_US 250
#define SPEED_Z_US 450
#define SPEED_Y_US 550
#define SPEED_HOMING_US 450

#define X_MAX_MM 1300.0f
#define Z_MAX_MM 1000.0f
#define Y_TRAVEL_MM 50.0f

#define SHELF_COLS 4
#define SHELF_ROWS 3

static const float SHELF_X_POS[SHELF_COLS] = {
  250.0f,
  490.0f,
  730.0f,
  970.0f
};

static const float SHELF_Z_POS[SHELF_ROWS] = {
  110.0f,
  360.0f,
  610.0f
};

#define CARGO_ENTRY_X_OFFSET_MM 17.0f
#define CARGO_ENTRY_TARGET_Z_MM 110.0f
#define PICKUP_BELOW_OFFSET_MM 60.0f
#define DROP_ABOVE_OFFSET_MM 60.0f
#define PICKUP_LIFT_MM 100.0f
#define DROP_DESCEND_MM 100.0f

#define SERIAL_BAUD_RATE 9600

#define DIR_POSITIVE HIGH
#define DIR_NEGATIVE LOW

#define MOTOR_ENABLE LOW
#define MOTOR_DISABLE HIGH

#define LIMIT_TRIGGERED LOW
#define LIMIT_NOT_TRIGGERED HIGH

// =========================
// serial_protocol.h
// =========================

#define CMD_STORE "STORE"
#define CMD_RETRIEVE "RETRIEVE"
#define CMD_HOME "HOME"
#define CMD_STATUS "STATUS"

#define RESP_READY "READY"
#define RESP_BUSY "BUSY"
#define RESP_OK "OK"
#define RESP_ERROR "ERR"

#define SERIAL_TIMEOUT_MS 100

enum class CommandType {
  NONE,
  STORE,
  RETRIEVE,
  HOME,
  STATUS,
  UNKNOWN
};

struct Command {
  CommandType type;
  uint8_t col;
  uint8_t row;
  bool valid;
};

// =========================
// stepper.h
// =========================

struct StepperMotor {
  uint8_t stepPin;
  uint8_t dirPin;
  uint8_t enablePin;
  long currentSteps;
  bool enabled;
};

StepperMotor motorX = {X_STEP_PIN, X_DIR_PIN, X_ENABLE_PIN, 0, false};
StepperMotor motorZ = {Z_STEP_PIN, Z_DIR_PIN, Z_ENABLE_PIN, 0, false};
StepperMotor motorY = {Y_STEP_PIN, Y_DIR_PIN, Y_ENABLE_PIN, 0, false};

// =========================
// Forward declarations
// =========================

void stepperInit(StepperMotor &motor);
void allSteppersInit(void);
void stepperEnable(StepperMotor &motor);
void stepperDisable(StepperMotor &motor);
void allSteppersDisable(void);
void stepperStep(StepperMotor &motor, uint8_t dir, unsigned int delayUs);
void stepperMoveSteps(StepperMotor &motor, long steps, uint8_t dir, unsigned int delayUs);
long mmToSteps(float mm);
float stepsToMm(long steps);

void axesInitLimitPins(void);
bool homeX(void);
bool homeZ(void);
bool homeAll(void);
void moveXTo(float targetMm, unsigned int delayUs = SPEED_X_US);
void moveZTo(float targetMm, unsigned int delayUs = SPEED_Z_US);
void moveYForward(unsigned int delayUs = SPEED_Y_US);
void moveYBack(unsigned int delayUs = SPEED_Y_US);
float getCurrentX(void);
float getCurrentZ(void);

bool storePackage(uint8_t col, uint8_t row);
bool retrievePackage(uint8_t col, uint8_t row);
bool isValidShelfPosition(uint8_t col, uint8_t row);

void serialProtocolInit(void);
bool serialReadCommand(Command &cmd);
void serialSendReady(void);
void serialSendBusy(void);
void serialSendOK(const char *msg = "");
void serialSendError(const char *msg);

// =========================
// stepper.cpp
// =========================

void stepperInit(StepperMotor &motor)
{
  pinMode(motor.stepPin, OUTPUT);
  pinMode(motor.dirPin, OUTPUT);
  pinMode(motor.enablePin, OUTPUT);

  digitalWrite(motor.enablePin, MOTOR_DISABLE);
  digitalWrite(motor.stepPin, LOW);
  digitalWrite(motor.dirPin, LOW);

  motor.currentSteps = 0;
  motor.enabled = false;
}

void allSteppersInit()
{
  stepperInit(motorX);
  stepperInit(motorZ);
  stepperInit(motorY);
}

void stepperEnable(StepperMotor &motor)
{
  digitalWrite(motor.enablePin, MOTOR_ENABLE);
  motor.enabled = true;
  delayMicroseconds(1000);
}

void stepperDisable(StepperMotor &motor)
{
  digitalWrite(motor.enablePin, MOTOR_DISABLE);
  motor.enabled = false;
}

void allSteppersDisable()
{
  stepperDisable(motorX);
  stepperDisable(motorZ);
  stepperDisable(motorY);
}

void stepperStep(StepperMotor &motor, uint8_t dir, unsigned int delayUs)
{
  digitalWrite(motor.dirPin, dir);
  delayMicroseconds(5);

  digitalWrite(motor.stepPin, HIGH);
  delayMicroseconds(5);
  digitalWrite(motor.stepPin, LOW);
  delayMicroseconds(delayUs);

  if (dir == DIR_POSITIVE)
    motor.currentSteps++;
  else
    motor.currentSteps--;
}

void stepperMoveSteps(StepperMotor &motor, long steps, uint8_t dir, unsigned int delayUs)
{
  if (steps <= 0)
    return;

  stepperEnable(motor);
  for (long i = 0; i < steps; i++)
    stepperStep(motor, dir, delayUs);
}

long mmToSteps(float mm)
{
  return (long)(mm * STEPS_PER_MM + 0.5f);
}

float stepsToMm(long steps)
{
  return (float)steps / STEPS_PER_MM;
}

// =========================
// axes.cpp
// =========================

static const long X_HOMING_MAX_STEPS = 249600L;
static const long Z_HOMING_MAX_STEPS = 192000L;

void axesInitLimitPins(void)
{
  pinMode(X_LIMIT_PIN, INPUT_PULLUP);
  pinMode(Z_LIMIT_PIN, INPUT_PULLUP);
}

bool homeX(void)
{
  long steps;

  stepperEnable(motorX);
  steps = 0;
  while (digitalRead(X_LIMIT_PIN) != LIMIT_TRIGGERED)
  {
    stepperStep(motorX, DIR_NEGATIVE, SPEED_HOMING_US);
    steps++;
  }
  motorX.currentSteps = 0;
  stepperDisable(motorX);
  delay(200);
  return true;
}

bool homeZ(void)
{
  long steps;

  stepperEnable(motorZ);
  steps = 0;
  while (digitalRead(Z_LIMIT_PIN) != LIMIT_TRIGGERED)
  {
    stepperStep(motorZ, DIR_NEGATIVE, SPEED_HOMING_US);
    steps++;
  }
  motorZ.currentSteps = 0;
  stepperDisable(motorZ);
  delay(200);
  return true;
}

bool homeAll(void)
{
  if (!homeZ())
    return false;
  delay(300);
  if (!homeX())
    return false;
  delay(300);
  return true;
}

void moveXTo(float targetMm, unsigned int delayUs)
{
  long targetSteps;
  long currentSteps;
  long delta;
  uint8_t dir;
  long abs_delta;

  if (targetMm < 0.0f)
    targetMm = 0.0f;
  if (targetMm > X_MAX_MM)
    targetMm = X_MAX_MM;
  targetSteps = mmToSteps(targetMm);
  currentSteps = motorX.currentSteps;
  delta = targetSteps - currentSteps;
  if (delta == 0)
    return;
  if (delta > 0)
    dir = DIR_POSITIVE;
  else
    dir = DIR_NEGATIVE;
  if (delta > 0)
    abs_delta = delta;
  else
    abs_delta = -delta;

  stepperMoveSteps(motorX, abs_delta, dir, delayUs);
  stepperDisable(motorX);
}

void moveZTo(float targetMm, unsigned int delayUs)
{
  long targetSteps;
  long currentSteps;
  long delta;
  uint8_t dir;
  long abs_delta;

  if (targetMm < 0.0f)
    targetMm = 0.0f;
  if (targetMm > Z_MAX_MM)
    targetMm = Z_MAX_MM;
  targetSteps = mmToSteps(targetMm);
  currentSteps = motorZ.currentSteps;
  delta = targetSteps - currentSteps;
  if (delta == 0)
    return;
  if (delta > 0)
    dir = DIR_POSITIVE;
  else
    dir = DIR_NEGATIVE;
  if (delta > 0)
    abs_delta = delta;
  else
    abs_delta = -delta;

  stepperMoveSteps(motorZ, abs_delta, dir, delayUs);
  stepperDisable(motorZ);
}

void moveYForward(unsigned int delayUs)
{
  long steps;

  steps = mmToSteps(Y_TRAVEL_MM);
  stepperMoveSteps(motorY, steps, DIR_POSITIVE, delayUs);
  stepperDisable(motorY);
  delay(100);
}

void moveYBack(unsigned int delayUs)
{
  long steps;

  steps = mmToSteps(Y_TRAVEL_MM);
  stepperMoveSteps(motorY, steps, DIR_NEGATIVE, delayUs);
  stepperDisable(motorY);
  delay(100);
}

float getCurrentX(void)
{
  return stepsToMm(motorX.currentSteps);
}

float getCurrentZ(void)
{
  return stepsToMm(motorZ.currentSteps);
}

// =========================
// operations.cpp
// =========================

static float clampZ(float z)
{
  if (z < 0.0f)
    return 0.0f;
  if (z > Z_MAX_MM)
    return Z_MAX_MM;
  return z;
}

bool isValidShelfPosition(uint8_t col, uint8_t row)
{
  if (col >= SHELF_COLS)
  {
    Serial.println(col);
    return false;
  }
  if (row >= SHELF_ROWS)
  {
    Serial.println(row);
    return false;
  }
  return true;
}

bool storePackage(uint8_t col, uint8_t row)
{
  float targetX;
  float targetZ;
  float cargoPickupStartZ;
  float shelfDropStartZ;

  if (!isValidShelfPosition(col, row))
    return false;
  if (!homeAll())
    return false;

  targetX = SHELF_X_POS[col];
  targetZ = SHELF_Z_POS[row];
  cargoPickupStartZ = clampZ(CARGO_ENTRY_TARGET_Z_MM - PICKUP_BELOW_OFFSET_MM);
  shelfDropStartZ = clampZ(targetZ + DROP_ABOVE_OFFSET_MM);

  // Kargo girisinden alma: home noktasindan X ekseninde 2.5 cm sola kay.
  moveXTo(CARGO_ENTRY_X_OFFSET_MM);
  moveZTo(cargoPickupStartZ);
  moveYForward();
  moveZTo(cargoPickupStartZ + PICKUP_LIFT_MM);
  moveYBack();

  moveXTo(targetX);

  // Rafa birakma: hedefin 6 cm ustunde bekleyip 10 cm asagi in.
  moveZTo(shelfDropStartZ);
  moveYForward();
  moveZTo(shelfDropStartZ - DROP_DESCEND_MM);
  moveYBack();

  if (!homeAll())
    return false;

  allSteppersDisable();
  return true;
}

bool retrievePackage(uint8_t col, uint8_t row)
{
  float targetX;
  float targetZ;
  float shelfPickupStartZ;
  float safeTransitZ;
  float cargoDropStartZ;

  if (!isValidShelfPosition(col, row))
    return false;
  if (!homeAll())
    return false;

  targetX = SHELF_X_POS[col];
  targetZ = SHELF_Z_POS[row];
  shelfPickupStartZ = clampZ(targetZ - PICKUP_BELOW_OFFSET_MM);
  safeTransitZ = clampZ(CARGO_ENTRY_TARGET_Z_MM - PICKUP_BELOW_OFFSET_MM);
  cargoDropStartZ = clampZ(CARGO_ENTRY_TARGET_Z_MM + DROP_ABOVE_OFFSET_MM);

  moveXTo(targetX);
  moveZTo(shelfPickupStartZ);

  // Raftan alma: hedefin 6 cm altindan yaklasip 10 cm yukari cikar.
  moveYForward();
  moveZTo(shelfPickupStartZ + PICKUP_LIFT_MM);
  moveYBack();

  // Raf yamuklugu nedeniyle: once Z'yi guvenli dusuk seviyeye indir, sonra X hareket etsin.
  moveZTo(safeTransitZ);

  // Kargo girisine birakma: X'te 2.5 cm sola kay.
  moveXTo(CARGO_ENTRY_X_OFFSET_MM);
  moveZTo(cargoDropStartZ);
  moveYForward();
  moveZTo(cargoDropStartZ - DROP_DESCEND_MM);
  moveYBack();

  if (!homeAll())
    return false;

  allSteppersDisable();
  return true;
}

// =========================
// serial_protocol.cpp
// =========================

static char rxBuffer[64];
static uint8_t rxIndex = 0;

void serialProtocolInit(void)
{
  Serial.begin(SERIAL_BAUD_RATE);
  while (!Serial)
  {
    ;
  }
  memset(rxBuffer, 0, sizeof(rxBuffer));
  rxIndex = 0;
}

bool serialReadCommand(Command &cmd)
{
  char c;
  String line;
  int firstColon;
  int secondColon;
  uint8_t col;
  uint8_t row;

  cmd = {CommandType::NONE, 0, 0, false};
  while (Serial.available() > 0)
  {
    c = (char)Serial.read();
    if (c == '\n' || c == '\r')
    {
      rxBuffer[rxIndex] = '\0';
      if (rxIndex > 0)
      {
        line = String(rxBuffer);
        line.trim();
        rxIndex = 0;
        memset(rxBuffer, 0, sizeof(rxBuffer));

        if (line.startsWith(CMD_STORE))
        {
          firstColon = line.indexOf(':');
          secondColon = line.indexOf(':', firstColon + 1);
          if (firstColon > 0 && secondColon > firstColon)
          {
            col = (uint8_t)line.substring(firstColon + 1, secondColon).toInt();
            row = (uint8_t)line.substring(secondColon + 1).toInt();
            cmd = {CommandType::STORE, col, row, true};
          }
          else
          {
            cmd.type = CommandType::UNKNOWN;
            cmd.valid = false;
          }
          return true;
        }
        else if (line.startsWith(CMD_RETRIEVE))
        {
          firstColon = line.indexOf(':');
          secondColon = line.indexOf(':', firstColon + 1);
          if (firstColon > 0 && secondColon > firstColon)
          {
            col = (uint8_t)line.substring(firstColon + 1, secondColon).toInt();
            row = (uint8_t)line.substring(secondColon + 1).toInt();
            cmd = {CommandType::RETRIEVE, col, row, true};
          }
          else
          {
            cmd.type = CommandType::UNKNOWN;
            cmd.valid = false;
          }
          return true;
        }
        else if (line.equals(CMD_HOME))
        {
          cmd = {CommandType::HOME, 0, 0, true};
          return true;
        }
        else if (line.equals(CMD_STATUS))
        {
          cmd = {CommandType::STATUS, 0, 0, true};
          return true;
        }
        else if (line.length() > 0)
        {
          cmd = {CommandType::UNKNOWN, 0, 0, false};
          return true;
        }
      }
      else
      {
        rxIndex = 0;
      }
    }
    else
    {
      if (rxIndex < (sizeof(rxBuffer) - 1))
        rxBuffer[rxIndex++] = c;
    }
  }
  return false;
}

void serialSendReady(void)
{
  Serial.println(F(RESP_READY));
}

void serialSendBusy(void)
{
  Serial.println(F(RESP_BUSY));
}

void serialSendOK(const char *msg)
{
  Serial.print(F(RESP_OK));
  if (msg && msg[0] != '\0')
  {
    Serial.print(':');
    Serial.print(msg);
  }
  Serial.println();
}

void serialSendError(const char *msg)
{
  Serial.print(F(RESP_ERROR));
  Serial.print(':');
  Serial.println(msg);
}

// =========================
// main.cpp
// =========================

static bool systemReady = false;

void setup(void)
{
  serialProtocolInit();
  allSteppersInit();
  axesInitLimitPins();

  if (homeAll())
  {
    systemReady = true;
    serialSendReady();
  }
  else
  {
    systemReady = false;
    serialSendError("HOMING_FAILED");
  }
}

//ERR:HOMING_FAILED

void loop(void)
{
  Command cmd;
  char buf[48];

  if (!serialReadCommand(cmd))
    return;

  if (!systemReady && cmd.type != CommandType::HOME)
  {
    serialSendError("SYSTEM_NOT_READY");
    return;
  }

  switch (cmd.type)
  {
  case CommandType::STORE:
    if (!cmd.valid || !isValidShelfPosition(cmd.col, cmd.row))
    {
      serialSendError("INVALID_POSITION");
      break;
    }
    serialSendBusy();
    if (storePackage(cmd.col, cmd.row))
    {
      systemReady = true;
      serialSendOK("STORE_DONE");
      serialSendReady();
    }
    else
    {
      systemReady = false;
      serialSendError("STORE_FAILED");
    }
    break;

  case CommandType::RETRIEVE:
    if (!cmd.valid || !isValidShelfPosition(cmd.col, cmd.row))
    {
      serialSendError("INVALID_POSITION");
      break;
    }
    serialSendBusy();
    if (retrievePackage(cmd.col, cmd.row))
    {
      systemReady = true;
      serialSendOK("RETRIEVE_DONE");
      serialSendReady();
    }
    else
    {
      systemReady = false;
      serialSendError("RETRIEVE_FAILED");
    }
    break;

  case CommandType::HOME:
    serialSendBusy();
    if (homeAll())
    {
      systemReady = true;
      serialSendOK("HOMED");
      serialSendReady();
    }
    else
    {
      systemReady = false;
      serialSendError("HOMING_FAILED");
    }
    break;

  case CommandType::STATUS:
    snprintf(buf, sizeof(buf), "X=%.1fmm Z=%.1fmm READY=%d", getCurrentX(), getCurrentZ(), (int)systemReady);
    serialSendOK(buf);
    break;

  default:
    serialSendError("UNKNOWN_CMD");
    break;
  }
}
