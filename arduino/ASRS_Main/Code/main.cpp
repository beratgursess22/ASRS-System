/**
 * ASRS_Main.ino
 * -------------
 * Otomatik Depolama ve Geri Alma Sistemi – Arduino Mega Ana Taslağı
 *
 * Görev akışı:
 *   1. Tüm modüller başlatılır (seri, motorlar, limit pinler).
 *   2. X ve Z eksenleri referans noktalarına (homing) götürülür.
 *   3. Raspberry Pi'ye "READY" yanıtı gönderilir.
 *   4. Ana döngüde USB seri üzerinden komut beklenir.
 *   5. Gelen STORE / RETRIEVE / HOME / STATUS komutları işlenir.
 *
 * Bağlantı:
 *   Raspberry Pi  ──USB──►  Arduino Mega (Serial @ 9600 baud)
 */

#include "../library/config.h"
#include "../library/axes.h"
#include "../library/operations.h"
#include "../library/serial_protocol.h"
#include "../library/stepper.h"

static bool	systemReady = false; // Homing tamamlanınca true olur

void	setup(void)
{
	serialProtocolInit();
	// Serial.println(F("============================================"));
	// Serial.println(F("  AS/RS Sistemi Baslatiliyor..."));
	// Serial.println(F("  Arduino Mega - v1.0"));
	// Serial.println(F("============================================"));
	allSteppersInit();
	// Serial.println(F("[INIT] Motorlar baslatildi."));
	axesInitLimitPins();
	// Serial.println(F("[INIT] Limit switch pinleri hazir."));
	// Serial.println(F("[INIT] Homing basliyor..."));
	if (homeAll())
	{
		systemReady = true;
		// Serial.println(F("[INIT] Homing basarili."));
		serialSendReady();
	}
	else
	{
		systemReady = false;
		// Serial.println(F("[HATA] Homing basarisiz! Sistemi kontrol edin."));
		serialSendError("HOMING_FAILED");
	}
}

void	loop(void)
{
	Command	cmd;
		char buf[48];

	if (!serialReadCommand(cmd)) // Seri hattan komut okunmaya çalışılır.
		return ;
	if (!systemReady && cmd.type != CommandType::HOME)
	{
		serialSendError("SYSTEM_NOT_READY");
		return ;
	}
	switch (cmd.type)
	{
	case CommandType::STORE:
		if (!cmd.valid || !isValidShelfPosition(cmd.col, cmd.row))
		{
			serialSendError("INVALID_POSITION");
			break ;
		}
		serialSendBusy();
		if (storePackage(cmd.col, cmd.row)) // depolama senaryosu çalıştırılır
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
		break ;
	case CommandType::RETRIEVE:
		if (!cmd.valid || !isValidShelfPosition(cmd.col, cmd.row))
		{
			serialSendError("INVALID_POSITION");
			break ;
		}
		serialSendBusy();
		if (retrievePackage(cmd.col, cmd.row)) // geri alma senaryosu çalıştırılır
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
		break ;
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
		break ;
	case CommandType::STATUS:
	{
		snprintf(buf, sizeof(buf), "X=%.1fmm Z=%.1fmm READY=%d", getCurrentX(),
			getCurrentZ(), (int)systemReady);
		serialSendOK(buf);
		break ;
	}
	default:
		serialSendError("UNKNOWN_CMD");
		break ;
	}
}
