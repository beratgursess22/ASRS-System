
#ifndef STEPPER_H
# define STEPPER_H

# include "config.h"
# include <Arduino.h>

struct				StepperMotor
{
	uint8_t			stepPin;
	uint8_t			dirPin;
	uint8_t			enablePin;
	long			currentSteps;
	bool			enabled;
};

extern StepperMotor	motorX;
extern StepperMotor	motorZ;
extern StepperMotor	motorY;

void				stepperInit(StepperMotor &motor);
void				allSteppersInit(void);
void				stepperEnable(StepperMotor &motor);
void				stepperDisable(StepperMotor &motor);
void				allSteppersDisable(void);
void				stepperStep(StepperMotor &motor, uint8_t dir,
						unsigned int delayUs);
void				stepperMoveSteps(StepperMotor &motor, long steps,
						uint8_t dir, unsigned int delayUs);
long				mmToSteps(float mm);
float				stepsToMm(long steps);

#endif
