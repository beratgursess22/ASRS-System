
#include "operations.h"

bool	isValidShelfPosition(uint8_t col, uint8_t row)
{
	if (col >= SHELF_COLS)
	{
		// Serial.print(F("[HATA] Gecersiz sutun: "));
		Serial.println(col);
		return (false);
	}
	if (row >= SHELF_ROWS)
	{
		// Serial.print(F("[HATA] Gecersiz kat: "));
		Serial.println(row);
		return (false);
	}
	return (true);
}

void	pickupFromEntryPoint(void)
{
	// Serial.println(F("--- Giris noktasindan paket aliyor ---"));
	moveYForward();
	delay(200);
	zLiftUp();
	delay(200);
	moveYBack();
	delay(200);
	// Serial.println(F("--- Paket alindi ---"));
}

void	placeOnShelf(void)
{
	// Serial.println(F("--- Raf gozune birakiliyor ---"));
	moveYForward();
	delay(200);
	zDropDown();
	delay(200);
	moveYBack();
	delay(200);
	// Serial.println(F("--- Paket birakildi ---"));
}

void	liftFromShelf(void)
{
	// Serial.println(F("--- Raftan paket kaldiriliyor ---"));
	moveYForward();
	delay(200);
	zLiftUp();
	delay(200);
	moveYBack();
	delay(200);
	// Serial.println(F("--- Paket raftan alindi ---"));
}

void	placeAtExitPoint(void)
{
	// Serial.println(F("--- Cikis noktasina birakiliyor ---"));
	moveYForward();
	delay(200);
	zDropDown();
	delay(200);
	moveYBack();
	delay(200);
	// Serial.println(F("--- Paket cikis noktasina birakildi ---"));
}

void	storePackage(uint8_t col, uint8_t row)
{
	float	targetX;
	float	targetZ;

	if (!isValidShelfPosition(col, row))
		return ;
	targetX = SHELF_X_POS[col];
	targetZ = SHELF_Z_POS[row];
	// DEBUG
	// Serial.print(F("=== DEPOLAMA: Sutun "));
	// Serial.print(col + 1);
	// Serial.print(F(", Kat "));
	// Serial.print(row + 1);
	// Serial.print(F(" | X="));
	// Serial.print(targetX, 0);
	// Serial.print(F("mm Z="));
	// Serial.print(targetZ, 0);
	// Serial.println(F("mm ==="));
	pickupFromEntryPoint();
	// Serial.println(F("[1] Hedef sutuna gidiliyor..."));
	moveXTo(targetX);
	// Serial.println(F("[2] Hedef kata cikiliyor..."));
	moveZTo(targetZ);
	// ── AŞAMA 4: Paketi rafa bırak ────────────────────────────────────────────
	placeOnShelf();
	// Serial.println(F("[4] Baslangic noktasina donuluyor..."));
	moveZTo(0.0f); // Önce Z'yi düşür (güvenlik)
	moveXTo(0.0f); // Sonra X'i sıfıra al
	allSteppersDisable();
	// Serial.println(F("=== DEPOLAMA TAMAMLANDI ==="));
}

void	retrievePackage(uint8_t col, uint8_t row)
{
	float	targetX;
	float	targetZ;

	if (!isValidShelfPosition(col, row))
		return ;
	targetX = SHELF_X_POS[col];
	targetZ = SHELF_Z_POS[row];
	// DEBUG
	// Serial.print(F("=== GERI ALMA: Sutun "));
	// Serial.print(col + 1);
	// Serial.print(F(", Kat "));
	// Serial.print(row + 1);
	// Serial.print(F(" | X="));
	// Serial.print(targetX, 0);
	// Serial.print(F("mm Z="));
	// Serial.print(targetZ, 0);
	// Serial.println(F("mm ==="));
	// Serial.println(F("[1] Hedef sutuna gidiliyor..."));
	moveXTo(targetX);
	// Serial.println(F("[2] Hedef kata cikiliyor..."));
	moveZTo(targetZ);
	// Serial.println(F("[3] Paketi raftan aliyor..."));
	liftFromShelf();
	// Serial.println(F("[4] Baslangic noktasina donuluyor..."));
	moveZTo(0.0f); // Önce Z'yi düşür
	moveXTo(0.0f); // Sonra X'i sıfıra al
	// Serial.println(F("[5] Paket cikis noktasina birakiliyor..."));
	placeAtExitPoint();
	allSteppersDisable();
	// Serial.println(F("=== GERI ALMA TAMAMLANDI ==="));
}
