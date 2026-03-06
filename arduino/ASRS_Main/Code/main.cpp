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

#include "../config.h"
#include "../library/stepper.h"
#include "../library/axes.h"
#include "../library/operations.h"
#include "../library/serial_protocol.h"

// ─── DURUM DEĞİŞKENİ ─────────────────────────────────────────────────────────

static bool systemReady = false; // Homing tamamlanınca true olur

// ─── SETUP ───────────────────────────────────────────────────────────────────

void setup() {
    // 1. Seri haberleşmeyi başlat
    serialProtocolInit();
    Serial.println(F("============================================"));
    Serial.println(F("  AS/RS Sistemi Baslatiliyor..."));
    Serial.println(F("  Arduino Mega - v1.0"));
    Serial.println(F("============================================"));

    // 2. Motor sürücülerini başlat
    allSteppersInit();
    Serial.println(F("[INIT] Motorlar baslatildi."));

    // 3. Limit switch pinlerini başlat
    axesInitLimitPins();
    Serial.println(F("[INIT] Limit switch pinleri hazir."));

    // 4. Homing – sistem açıldığında tüm eksenler referanslanır
    Serial.println(F("[INIT] Homing basliyor..."));
    if (homeAll()) {
        systemReady = true;
        Serial.println(F("[INIT] Homing basarili."));
        serialSendReady();
    } else {
        systemReady = false;
        Serial.println(F("[HATA] Homing basarisiz! Sistemi kontrol edin."));
        serialSendError("HOMING_FAILED");
    }
}

// ─── ANA DÖNGÜ ────────────────────────────────────────────────────────────────

void loop() {
    Command cmd;

    // Seri hattan komut geldi mi?
    if (!serialReadCommand(cmd)) return;

    // Sistem hazır değilse tüm komutları reddet
    if (!systemReady && cmd.type != CommandType::HOME) {
        serialSendError("SYSTEM_NOT_READY");
        return;
    }

    // ─── Komut İşleme ────────────────────────────────────────────────────────

    switch (cmd.type) {

        // ── DEPOLAMA ─────────────────────────────────────────────────────────
        case CommandType::STORE:
            if (!cmd.valid || !isValidShelfPosition(cmd.col, cmd.row)) {
                serialSendError("INVALID_POSITION");
                break;
            }
            serialSendBusy();
            storePackage(cmd.col, cmd.row);
            serialSendOK("STORE_DONE");
            serialSendReady();
            break;

        // ── GERİ ALMA ─────────────────────────────────────────────────────────
        case CommandType::RETRIEVE:
            if (!cmd.valid || !isValidShelfPosition(cmd.col, cmd.row)) {
                serialSendError("INVALID_POSITION");
                break;
            }
            serialSendBusy();
            retrievePackage(cmd.col, cmd.row);
            serialSendOK("RETRIEVE_DONE");
            serialSendReady();
            break;

        // ── HOMING ────────────────────────────────────────────────────────────
        case CommandType::HOME:
            serialSendBusy();
            if (homeAll()) {
                systemReady = true;
                serialSendOK("HOMED");
                serialSendReady();
            } else {
                systemReady = false;
                serialSendError("HOMING_FAILED");
            }
            break;

        // ── DURUM SORGULAMA ───────────────────────────────────────────────────
        case CommandType::STATUS: {
            char buf[48];
            snprintf(buf, sizeof(buf), "X=%.1fmm Z=%.1fmm READY=%d",
                     getCurrentX(), getCurrentZ(), (int)systemReady);
            serialSendOK(buf);
            break;
        }

        // ── BİLİNMEYEN KOMUT ─────────────────────────────────────────────────
        default:
            serialSendError("UNKNOWN_CMD");
            break;
    }
}
