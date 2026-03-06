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
#define SERIAL_PROTOCOL_H

#include <Arduino.h>

// ─── PAROTOKOl SABİTLERİ ─────────────────────────────────────────────────────

#define CMD_STORE       "STORE"
#define CMD_RETRIEVE    "RETRIEVE"
#define CMD_HOME        "HOME"
#define CMD_STATUS      "STATUS"

#define RESP_READY      "READY"
#define RESP_BUSY       "BUSY"
#define RESP_OK         "OK"
#define RESP_ERROR      "ERR"

#define SERIAL_TIMEOUT_MS   100   // Komut parçaları arası maksimum bekleme

// ─── KOMUT YAPISI ────────────────────────────────────────────────────────────

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
    uint8_t     col;   // 0–3
    uint8_t     row;   // 0–2
    bool        valid;
};

// ─── FONKSİYON PROTOTİPLERİ ──────────────────────────────────────────────────

/**
 * @brief Seri portu başlatır.
 */
void serialProtocolInit();

/**
 * @brief Seri tamponda bekleyen tam bir satır var mı kontrol eder.
 *        Satır varsa komut yapısına ayrıştırır.
 *
 * @param cmd  Doldurulacak komut yapısı
 * @return     true: geçerli komut geldi, false: henüz tam komut yok
 */
bool serialReadCommand(Command &cmd);

/**
 * @brief "READY\n" yanıtı gönderir.
 */
void serialSendReady();

/**
 * @brief "BUSY\n" yanıtı gönderir.
 */
void serialSendBusy();

/**
 * @brief "OK:<mesaj>\n" yanıtı gönderir.
 * @param msg  Ek açıklama mesajı
 */
void serialSendOK(const char *msg = "");

/**
 * @brief "ERR:<mesaj>\n" yanıtı gönderir.
 * @param msg  Hata açıklaması
 */
void serialSendError(const char *msg);

#endif // SERIAL_PROTOCOL_H
