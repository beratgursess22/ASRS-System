/**
 * serial_protocol.cpp
 * -------------------
 * Raspberry Pi ↔ Arduino seri protokolü implementasyonu.
 */

#include "serial_protocol.h"
#include "config.h"

// Satır tamponu
static char    rxBuffer[64];
static uint8_t rxIndex = 0;

// ─── BAŞLATMA ────────────────────────────────────────────────────────────────

void serialProtocolInit() {
    Serial.begin(SERIAL_BAUD_RATE);
    while (!Serial) { ; } // Leonardo/Micro için – Mega'da hemen geçer
    memset(rxBuffer, 0, sizeof(rxBuffer));
    rxIndex = 0;
}

// ─── KOMUT OKUMAYI ────────────────────────────────────────────────────────────

bool serialReadCommand(Command &cmd) {
    cmd = {CommandType::NONE, 0, 0, false};

    // Seri tampondan mevcut karakterleri oku
    while (Serial.available() > 0) {
        char c = (char)Serial.read();

        if (c == '\n' || c == '\r') {
            // Satır sonu: tamponu işle
            rxBuffer[rxIndex] = '\0';

            if (rxIndex > 0) {
                // Tamponu ayrıştır
                String line = String(rxBuffer);
                line.trim();
                rxIndex = 0;
                memset(rxBuffer, 0, sizeof(rxBuffer));

                // ─── STORE:<col>:<row> ─────────────────────────────────────
                if (line.startsWith(CMD_STORE)) {
                    // "STORE:col:row"
                    int firstColon  = line.indexOf(':');
                    int secondColon = line.indexOf(':', firstColon + 1);

                    if (firstColon > 0 && secondColon > firstColon) {
                        uint8_t col = (uint8_t)line.substring(firstColon + 1, secondColon).toInt();
                        uint8_t row = (uint8_t)line.substring(secondColon + 1).toInt();
                        cmd = {CommandType::STORE, col, row, true};
                    } else {
                        cmd.type  = CommandType::UNKNOWN;
                        cmd.valid = false;
                    }
                    return true;
                }

                // ─── RETRIEVE:<col>:<row> ──────────────────────────────────
                else if (line.startsWith(CMD_RETRIEVE)) {
                    int firstColon  = line.indexOf(':');
                    int secondColon = line.indexOf(':', firstColon + 1);

                    if (firstColon > 0 && secondColon > firstColon) {
                        uint8_t col = (uint8_t)line.substring(firstColon + 1, secondColon).toInt();
                        uint8_t row = (uint8_t)line.substring(secondColon + 1).toInt();
                        cmd = {CommandType::RETRIEVE, col, row, true};
                    } else {
                        cmd.type  = CommandType::UNKNOWN;
                        cmd.valid = false;
                    }
                    return true;
                }

                // ─── HOME ──────────────────────────────────────────────────
                else if (line.equals(CMD_HOME)) {
                    cmd = {CommandType::HOME, 0, 0, true};
                    return true;
                }

                // ─── STATUS ────────────────────────────────────────────────
                else if (line.equals(CMD_STATUS)) {
                    cmd = {CommandType::STATUS, 0, 0, true};
                    return true;
                }

                // ─── Bilinmeyen komut ──────────────────────────────────────
                else if (line.length() > 0) {
                    cmd = {CommandType::UNKNOWN, 0, 0, false};
                    return true;
                }
            } else {
                rxIndex = 0;
            }

        } else {
            // Tampona ekle (taşma koruması)
            if (rxIndex < (sizeof(rxBuffer) - 1)) {
                rxBuffer[rxIndex++] = c;
            }
        }
    }

    return false; // Henüz tam satır yok
}

// ─── YANIT GÖNDERİMİ ─────────────────────────────────────────────────────────

void serialSendReady() {
    Serial.println(F(RESP_READY));
}

void serialSendBusy() {
    Serial.println(F(RESP_BUSY));
}

void serialSendOK(const char *msg) {
    Serial.print(F(RESP_OK));
    if (msg && msg[0] != '\0') {
        Serial.print(':');
        Serial.print(msg);
    }
    Serial.println();
}

void serialSendError(const char *msg) {
    Serial.print(F(RESP_ERROR));
    Serial.print(':');
    Serial.println(msg);
}
