/**
 * operations.cpp
 * --------------
 * Depolama ve geri alma senaryolarının implementasyonu.
 */

#include "operations.h"

// ─── YARDIMCI ────────────────────────────────────────────────────────────────

bool isValidShelfPosition(uint8_t col, uint8_t row) {
    if (col >= SHELF_COLS) {
        Serial.print(F("[HATA] Gecersiz sutun: "));
        Serial.println(col);
        return false;
    }
    if (row >= SHELF_ROWS) {
        Serial.print(F("[HATA] Gecersiz kat: "));
        Serial.println(row);
        return false;
    }
    return true;
}

// ─── PAKET GİRİŞ NOKTASINDAN ALMA YARDIMCISI ─────────────────────────────────
// Sistem giriş/çıkış noktasındayken (X=0, Z=0) paketi shuttle'a yükler.
static void pickupFromEntryPoint() {
    Serial.println(F("--- Giris noktasindan paket aliyor ---"));

    // 1. Y ile pakete uzan
    moveYForward();
    delay(200);

    // 2. Paketi kavramak için hafifçe yukarı kaldır (mekanik yapıya göre)
    zLiftUp();
    delay(200);

    // 3. Shuttle'ı geri çek (paket shuttle üzerinde)
    moveYBack();
    delay(200);

    Serial.println(F("--- Paket alindi ---"));
}

// ─── PAKETI RAF GÖZÜNE BIRAKMA YARDIMCISI ────────────────────────────────────
// Sistem hedef konumdayken (X=hedef, Z=hedef) paketi rafa yerleştirir.
static void placeOnShelf() {
    Serial.println(F("--- Raf gozune birakiliyor ---"));

    // 1. Y ile raf içine uzan
    moveYForward();
    delay(200);

    // 2. Paketi bırakmak için hafifçe aşağı indir
    zDropDown();
    delay(200);

    // 3. Shuttle'ı geri çek (paket rafta kalır)
    moveYBack();
    delay(200);

    Serial.println(F("--- Paket birakildi ---"));
}

// ─── PAKETI RAFTAN KALDIRMA YARDIMCISI ───────────────────────────────────────
// Sistem hedef konumdayken (X=hedef, Z=hedef) raftan paketi alır.
static void liftFromShelf() {
    Serial.println(F("--- Raftan paket kaldiriliyor ---"));

    // 1. Y ile raf içine uzan (pakete ulaş)
    moveYForward();
    delay(200);

    // 2. Paketi kaldırmak için Z'de yukarı hareket
    zLiftUp();
    delay(200);

    // 3. Shuttle'ı geri çek (paket shuttle üzerinde)
    moveYBack();
    delay(200);

    Serial.println(F("--- Paket raftan alindi ---"));
}

// ─── PAKETI ÇIKIŞ NOKTASINA BIRAKMA YARDIMCISI ───────────────────────────────
// Sistem giriş/çıkış noktasına döndükten sonra paketi konveyöre/masaya bırakır.
static void placeAtExitPoint() {
    Serial.println(F("--- Cikis noktasina birakiliyor ---"));

    // 1. Y ile ileri uzan
    moveYForward();
    delay(200);

    // 2. Paketi alta indir (bırak)
    zDropDown();
    delay(200);

    // 3. Shuttle'ı geri çek
    moveYBack();
    delay(200);

    Serial.println(F("--- Paket cikis noktasina birakildi ---"));
}

// ─── DEPOLAMA SENARYOSU ───────────────────────────────────────────────────────

void storePackage(uint8_t col, uint8_t row) {
    if (!isValidShelfPosition(col, row)) return;

    float targetX = SHELF_X_POS[col];
    float targetZ = SHELF_Z_POS[row];

    Serial.print(F("=== DEPOLAMA: Sutun "));
    Serial.print(col + 1);
    Serial.print(F(", Kat "));
    Serial.print(row + 1);
    Serial.print(F(" | X="));
    Serial.print(targetX, 0);
    Serial.print(F("mm Z="));
    Serial.print(targetZ, 0);
    Serial.println(F("mm ==="));

    // ── AŞAMA 1: Giriş noktasından paketi al ─────────────────────────────────
    // Sistem zaten X=0, Z=0'da (referans konumu)
    pickupFromEntryPoint();

    // ── AŞAMA 2: X ekseninde hedef sütuna git ─────────────────────────────────
    Serial.println(F("[1] Hedef sutuna gidiliyor..."));
    moveXTo(targetX);

    // ── AŞAMA 3: Z ekseninde hedef kata çık ──────────────────────────────────
    Serial.println(F("[2] Hedef kata cikiliyor..."));
    moveZTo(targetZ);

    // ── AŞAMA 4: Paketi rafa bırak ────────────────────────────────────────────
    Serial.println(F("[3] Paket rafa birakiliyor..."));
    placeOnShelf();

    // ── AŞAMA 5: Başlangıç noktasına dön ─────────────────────────────────────
    Serial.println(F("[4] Baslangic noktasina donuluyor..."));
    moveZTo(0.0f);   // Önce Z'yi düşür (güvenlik)
    moveXTo(0.0f);   // Sonra X'i sıfıra al

    allSteppersDisable();
    Serial.println(F("=== DEPOLAMA TAMAMLANDI ==="));
}

// ─── GERİ ALMA SENARYOSU ──────────────────────────────────────────────────────

void retrievePackage(uint8_t col, uint8_t row) {
    if (!isValidShelfPosition(col, row)) return;

    float targetX = SHELF_X_POS[col];
    float targetZ = SHELF_Z_POS[row];

    Serial.print(F("=== GERI ALMA: Sutun "));
    Serial.print(col + 1);
    Serial.print(F(", Kat "));
    Serial.print(row + 1);
    Serial.print(F(" | X="));
    Serial.print(targetX, 0);
    Serial.print(F("mm Z="));
    Serial.print(targetZ, 0);
    Serial.println(F("mm ==="));

    // ── AŞAMA 1: X ekseninde hedef sütuna git ─────────────────────────────────
    Serial.println(F("[1] Hedef sutuna gidiliyor..."));
    moveXTo(targetX);

    // ── AŞAMA 2: Z ekseninde hedef kata çık ──────────────────────────────────
    Serial.println(F("[2] Hedef kata cikiliyor..."));
    moveZTo(targetZ);

    // ── AŞAMA 3: Raftan paketi kaldır ────────────────────────────────────────
    Serial.println(F("[3] Paketi raftan aliyor..."));
    liftFromShelf();

    // ── AŞAMA 4: Başlangıç konumuna dön ──────────────────────────────────────
    Serial.println(F("[4] Baslangic noktasina donuluyor..."));
    moveZTo(0.0f);   // Önce Z'yi düşür
    moveXTo(0.0f);   // Sonra X'i sıfıra al

    // ── AŞAMA 5: Paketi çıkış noktasına bırak ────────────────────────────────
    Serial.println(F("[5] Paket cikis noktasina birakiliyor..."));
    placeAtExitPoint();

    allSteppersDisable();
    Serial.println(F("=== GERI ALMA TAMAMLANDI ==="));
}
