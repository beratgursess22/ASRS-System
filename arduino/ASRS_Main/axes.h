
#ifndef AXES_H
# define AXES_H

# include "config.h"
# include "stepper.h"
# include <Arduino.h>

void	axesInitLimitPins(void);
bool	homeX(void);
bool	homeZ(void);
bool	homeAll(void);
void	moveXTo(float targetMm, unsigned int delayUs = SPEED_NORMAL_US);
void	moveZTo(float targetMm, unsigned int delayUs = SPEED_NORMAL_US);
void	moveYForward(unsigned int delayUs = SPEED_SLOW_US);
void	moveYBack(unsigned int delayUs = SPEED_SLOW_US);
void	zDropDown(void);
void	zLiftUp(void);
float	getCurrentX(void);
float	getCurrentZ(void);

#endif
