#include "axes.h"

// Homing işleminde beklenen maksimum adım sayısı (güvenlik timeout)
// X: 1300 mm × 80 adım/mm × 1.2 güvenlik katsayısı ≈ 124800
// Z:  1000 mm × 80 adım/mm × 1.2 güvenlik katsayısı ≈ 96000
static const long	X_HOMING_MAX_STEPS = 124800L;
static const long	Z_HOMING_MAX_STEPS = 96000L;

void	axesInitLimitPins(void)
{
	pinMode(X_LIMIT_PIN, INPUT_PULLUP);
	pinMode(Z_LIMIT_PIN, INPUT_PULLUP);
}

bool	homeX(void)
{
	long	steps;

	// Serial.println(F("[HOMING] X ekseni referanslaniyor..."));
	stepperEnable(motorX);
	steps = 0;
	while (digitalRead(X_LIMIT_PIN) != LIMIT_TRIGGERED)
	{
		stepperStep(motorX, DIR_NEGATIVE, SPEED_HOMING_US);
		steps++;
		if (steps > X_HOMING_MAX_STEPS)
		{
			// Serial.println(F("[HATA] X homing timeout!"));
			stepperDisable(motorX);
			return (false);
		}
	}
	motorX.currentSteps = 0;
	stepperDisable(motorX);
	// Serial.println(F("[HOMING] X = 0 ayarlandi."));
	delay(200);
	return (true);
}

bool	homeZ(void)
{
	long	steps;

	// Serial.println(F("[HOMING] Z ekseni referanslaniyor..."));
	stepperEnable(motorZ);
	steps = 0;
	while (digitalRead(Z_LIMIT_PIN) != LIMIT_TRIGGERED)
	{
		stepperStep(motorZ, DIR_NEGATIVE, SPEED_HOMING_US);
		steps++;
		if (steps > Z_HOMING_MAX_STEPS)
		{
			// Serial.println(F("[HATA] Z homing timeout!"));
			stepperDisable(motorZ);
			return (false);
		}
	}
	motorZ.currentSteps = 0;
	stepperDisable(motorZ);
	// Serial.println(F("[HOMING] Z = 0 ayarlandi."));
	delay(200);
	return (true);
}

bool	homeAll(void)
{
	// Serial.println(F("[HOMING] Tum eksenler referanslanıyor..."));
	if (!homeZ())
		return (false);
	delay(300);
	if (!homeX())
		return (false);
	delay(300);
	// Serial.println(F("[HOMING] Tum eksenler hazir."));
	return (true);
}

void	moveXTo(float targetMm, unsigned int delayUs)
{
	long	targetSteps;
	long	currentSteps;
	long	delta;
	uint8_t	dir;
	long	abs_delta;

	if (targetMm < 0.0f)
		targetMm = 0.0f;
	if (targetMm > X_MAX_MM)
		targetMm = X_MAX_MM;
	targetSteps = mmToSteps(targetMm);
	currentSteps = motorX.currentSteps;
	delta = targetSteps - currentSteps;
	if (delta == 0)
		return ;
	if (delta > 0)
		dir = DIR_POSITIVE;
	else
		dir = DIR_NEGATIVE;
	if (delta > 0)
		abs_delta = delta;
	else
		abs_delta = -delta;
	// Debug
	// Serial.print(F("[X] "));
	// Serial.print(stepsToMm(currentSteps), 1);
	// Serial.print(F(" mm -> "));
	// Serial.print(targetMm, 1);
	// Serial.println(F(" mm"));
	stepperMoveSteps(motorX, abs_delta, dir, delayUs);
	stepperDisable(motorX);
}

void	moveZTo(float targetMm, unsigned int delayUs)
{
	long	targetSteps;
	long	currentSteps;
	long	delta;
	uint8_t	dir;
	long	abs_delta;

	if (targetMm < 0.0f)
		targetMm = 0.0f;
	if (targetMm > Z_MAX_MM)
		targetMm = Z_MAX_MM;
	targetSteps = mmToSteps(targetMm);
	currentSteps = motorZ.currentSteps;
	delta = targetSteps - currentSteps;
	if (delta == 0)
		return ;
	if (delta > 0)
		dir = DIR_POSITIVE;
	else
		dir = DIR_NEGATIVE;
	if (delta > 0)
		abs_delta = delta;
	else
		abs_delta = -delta;
	// Debug
	// Serial.print(F("[Z] "));
	// Serial.print(stepsToMm(currentSteps), 1);
	// Serial.print(F(" mm -> "));
	// Serial.print(targetMm, 1);
	// Serial.println(F(" mm"));
	stepperMoveSteps(motorZ, abs_delta, dir, delayUs);
	stepperDisable(motorZ);
}

void	moveYForward(unsigned int delayUs)
{
	long	steps;

	steps = mmToSteps(Y_TRAVEL_MM);
	// Serial.println(F("[Y] Shuttle ileri ->"));
	stepperMoveSteps(motorY, steps, DIR_POSITIVE, delayUs);
	stepperDisable(motorY);
	delay(100);
}

void	moveYBack(unsigned int delayUs)
{
	long	steps;

	steps = mmToSteps(Y_TRAVEL_MM);
	// Serial.println(F("[Y] Shuttle geri <-"));
	stepperMoveSteps(motorY, steps, DIR_NEGATIVE, delayUs);
	stepperDisable(motorY);
	delay(100);
}

void	zDropDown(void)
{
	long	steps;

	steps = mmToSteps(Z_DROP_LIFT_MM);
	// Serial.println(F("[Z] Birakma hareketi (asagi)"));
	stepperMoveSteps(motorZ, steps, DIR_NEGATIVE, SPEED_SLOW_US);
	motorZ.currentSteps -= steps;
	stepperDisable(motorZ);
	delay(100);
}

void	zLiftUp(void)
{
	long	steps;

	steps = mmToSteps(Z_DROP_LIFT_MM);
	// Serial.println(F("[Z] Kaldirma hareketi (yukari)"));
	stepperMoveSteps(motorZ, steps, DIR_POSITIVE, SPEED_SLOW_US);
	stepperDisable(motorZ);
	delay(100);
}

float	getCurrentX(void)
{
	return (stepsToMm(motorX.currentSteps));
}

float	getCurrentZ(void)
{
	return (stepsToMm(motorZ.currentSteps));
}
