/**
 * operations.h
 * ------------
 * Depolama (store) ve geri alma (retrieve) üst düzey hareket senaryoları.
 *
 * Her senaryo, axes.h üzerinden eksenleri sıralı bir şekilde hareket
 * ettirir. Raf konumu sütun (col) ve kat (row) indeksleriyle tanımlanır:
 *   col: 0–3 (0 = en sol sütun, SHELF_X_POS[0] = 160 mm)
 *   row: 0–2 (0 = en alt kat,    SHELF_Z_POS[0] = 250 mm)
 */

#ifndef OPERATIONS_H
#define OPERATIONS_H

#include <Arduino.h>
#include "config.h"
#include "axes.h"


void storePackage(uint8_t col, uint8_t row);
void retrievePackage(uint8_t col, uint8_t row);
void	pickupFromEntryPoint(void);
void	placeOnShelf(void);
void	liftFromShelf(void);
void	placeAtExitPoint(void);
bool isValidShelfPosition(uint8_t col, uint8_t row);

#endif 
