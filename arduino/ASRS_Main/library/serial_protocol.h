/**
 * serial_protocol.h
 * -----------------
 * Raspberry Pi ↔ Arduino Mega USB seri haberleşme protokolü.
 *
 * Protokol formatı (metin tabanlı, satır sonu: \n):
 *
 *   Raspberry Pi → Arduino:
 *     STORE:<col>:<row>\n      → Paketi belirtilen rafa depola
 *     RETRIEVE:<col>:<row>\n   → Belirtilen raftan paketi al
 *     HOME\n                   → Tüm eksenleri referans al
 *     STATUS\n                 → Sistem durumunu sorgula
 *
 *   Arduino → Raspberry Pi:
 *     READY\n                  → Hazır, komut bekliyor
 *     BUSY\n                   → İşlem devam ediyor
 *     OK:<mesaj>\n             → İşlem başarılı
 *     ERR:<mesaj>\n            → Hata oluştu
 *
 *   Sütun (col): 0–3, Kat (row): 0–2
 *   Örnek: "STORE:2:1\n" → Sütun 3, Kat 2'ye depola
 */

#ifndef SERIAL_PROTOCOL_H
# define SERIAL_PROTOCOL_H

# include <Arduino.h>

# define CMD_STORE "STORE"
# define CMD_RETRIEVE "RETRIEVE"
# define CMD_HOME "HOME"
# define CMD_STATUS "STATUS"

# define RESP_READY "READY"
# define RESP_BUSY "BUSY"
# define RESP_OK "OK"
# define RESP_ERROR "ERR"

# define SERIAL_TIMEOUT_MS 100

enum class CommandType
{
	NONE,
	STORE,
	RETRIEVE,
	HOME,
	STATUS,
	UNKNOWN
};
struct			Command
{
	CommandType	type;
	uint8_t		col;
	uint8_t		row;
	bool		valid;
};

void			serialProtocolInit(void);
bool			serialReadCommand(Command &cmd);
void			serialSendReady(void);
void			serialSendBusy(void);
void			serialSendOK(const char *msg = "");
void			serialSendError(const char *msg);

#endif
