
#include "../library/operations.h"

static float	approachBelow(float targetZ)
{
	float	z;

	z = targetZ - Z_APPROACH_OFFSET_MM;
	if (z < 0.0f)
		return (0.0f);
	return (z);
}

static float	approachAbove(float targetZ)
{
	float	z;

	z = targetZ + Z_APPROACH_OFFSET_MM;
	if (z > Z_MAX_MM)
		return (Z_MAX_MM);
	return (z);
}

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

bool	storePackage(uint8_t col, uint8_t row)
{
	float	targetX;
	float	targetZ;
	float	entryApproachZ;
	float	shelfApproachZ;

	if (!isValidShelfPosition(col, row))
		return (false);
	if (!homeAll())
		return (false);
	targetX = SHELF_X_POS[col];
	targetZ = SHELF_Z_POS[row];
	entryApproachZ = approachBelow(ENTRY_PICK_TARGET_Z_MM);
	shelfApproachZ = approachAbove(targetZ);
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
	// [1] Giris/teslim bolgesinde urunu alma (yaklasma asagidan)
	moveZTo(entryApproachZ);
	moveYForward();
	moveZTo(ENTRY_PICK_TARGET_Z_MM);
	moveYBack();
	// [2] Hedef sutuna git
	moveXTo(targetX);
	// [3] Hedef kata birakma icin yukaridan yaklas
	moveZTo(shelfApproachZ);
	// [4] Rafin icine gir ve urunu birak
	moveYForward();
	moveZTo(targetZ);
	moveYBack();
	// [5] Islem bitisi: tum eksenler tekrar home
	if (!homeAll())
		return (false);
	allSteppersDisable();
	// Serial.println(F("=== DEPOLAMA TAMAMLANDI ==="));
	return (true);
}

bool	retrievePackage(uint8_t col, uint8_t row)
{
	float	targetX;
	float	targetZ;
	float	shelfApproachZ;
	float	exitApproachZ;

	if (!isValidShelfPosition(col, row))
		return (false);
	targetX = SHELF_X_POS[col];
	targetZ = SHELF_Z_POS[row];
	shelfApproachZ = approachBelow(targetZ);
	exitApproachZ = approachAbove(EXIT_DROP_TARGET_Z_MM);
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
	// Serial.println(F("[2] Hedef kata asagidan yaklasiliyor..."));
	moveZTo(shelfApproachZ);
	// Serial.println(F("[3] Raf icine giriliyor ve urun aliniyor..."));
	moveYForward();
	moveZTo(targetZ);
	moveYBack();
	// Serial.println(F("[4] Urun giris/teslim bolgesine getiriliyor..."));
	if (!homeX())
		return (false);
	// Serial.println(F("[5] Teslim noktasinda yukaridan yaklasiliyor..."));
	moveZTo(exitApproachZ);
	moveYForward();
	moveZTo(EXIT_DROP_TARGET_Z_MM);
	moveYBack();
	if (!homeZ())
		return (false);
	allSteppersDisable();
	// Serial.println(F("=== GERI ALMA TAMAMLANDI ==="));
	return (true);
}
