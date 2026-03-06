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

// ─── SENARYO FONKSİYONLARI ───────────────────────────────────────────────────

/**
 * @brief Giriş/çıkış noktasındaki paketi, verilen raf gözüne depolar.
 *
 * Adım sırası:
 *  1. Paketi giriş noktasından al  (Y ileri → Z yukarı → Y geri)
 *  2. Hedef sütuna git             (X sola)
 *  3. Hedef kata çık               (Z yukarı)
 *  4. Paketi rafa bırak             (Y ileri → Z aşağı → Y geri)
 *  5. Başlangıç konumuna dön        (Z aşağı → X sağa)
 *
 * @param col  Hedef sütun indeksi (0–3)
 * @param row  Hedef kat indeksi   (0–2)
 */
void storePackage(uint8_t col, uint8_t row);

/**
 * @brief Verilen raf gözündeki paketi alarak giriş/çıkış noktasına getirir.
 *
 * Adım sırası:
 *  1. Hedef sütuna git             (X sola)
 *  2. Hedef kata çık               (Z yukarı)
 *  3. Raf içinden paketi al         (Y ileri → Z yukarı → Y geri)
 *  4. Başlangıç noktasına dön       (Z aşağı → X sağa)
 *  5. Paketi giriş/çıkış noktasına bırak (Y ileri → Z aşağı → Y geri)
 *
 * @param col  Kaynak sütun indeksi (0–3)
 * @param row  Kaynak kat indeksi   (0–2)
 */
void retrievePackage(uint8_t col, uint8_t row);

/**
 * @brief İndeks değerlerinin geçerli aralıkta olup olmadığını kontrol eder.
 * @param col  Sütun indeksi
 * @param row  Kat indeksi
 * @return     true: geçerli, false: sınır dışı
 */
bool isValidShelfPosition(uint8_t col, uint8_t row);

#endif // OPERATIONS_H
