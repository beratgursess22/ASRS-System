
#include "stepper.h"


StepperMotor motorX = {X_STEP_PIN, X_DIR_PIN, X_ENABLE_PIN, 0, false};
StepperMotor motorZ = {Z_STEP_PIN, Z_DIR_PIN, Z_ENABLE_PIN, 0, false};
StepperMotor motorY = {Y_STEP_PIN, Y_DIR_PIN, Y_ENABLE_PIN, 0, false};


void stepperInit(StepperMotor &motor) 
{
    pinMode(motor.stepPin,   OUTPUT);
    pinMode(motor.dirPin,    OUTPUT);
    pinMode(motor.enablePin, OUTPUT);

    digitalWrite(motor.enablePin, MOTOR_DISABLE);
    digitalWrite(motor.stepPin,   LOW);
    digitalWrite(motor.dirPin,    LOW);

    motor.currentSteps = 0;
    motor.enabled      = false;
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


void stepperStep(StepperMotor &motor, uint8_t dir, unsigned int delayUs) {
    digitalWrite(motor.dirPin, dir);
    delayMicroseconds(5); /

    digitalWrite(motor.stepPin, HIGH);
    delayMicroseconds(5);
    digitalWrite(motor.stepPin, LOW);
    delayMicroseconds(delayUs);

    if (dir == DIR_POSITIVE) 
        motor.currentSteps++;
    else 
        motor.currentSteps--;
}


void stepperMoveSteps(StepperMotor &motor, long steps, uint8_t dir, unsigned int delayUs) {
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
