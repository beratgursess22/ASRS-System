/**
 * config.h
 * --------
 * Tüm sabit değerler, pin tanımları, motor parametreleri ve
 * raf konumları bu dosyada merkezi olarak tanımlanmıştır.
 *
 * Donanım:
 *   - Arduino Mega 2560
 *   - 3x A4988 Step Motor Sürücü (1/16 mikro adım)
 *   - 3x NEMA 17 Step Motor (1.8°/adım = 200 adım/tur)
 *   - 20 dişli GT2 kasnak (40 mm/tur)
 *   - GT2 kayış (2 mm diş aralığı)
 *
 * Hesaplama:
 *   Adım/mm = (200 adım/tur × 16 mikro adım) / 40 mm = 80 adım/mm
 */

#ifndef CONFIG_H
#define CONFIG_H

// ─── PIN TANIMLARI ───────────────────────────────────────────────────────────

// X Ekseni (Yatay – Sağ/Sol)
#define X_STEP_PIN      2 // Step pini
#define X_DIR_PIN       3 // Yön pini
#define X_ENABLE_PIN    4 // Enable pini
#define X_LIMIT_PIN     5 // Sağ uç limit switch (X = 0 referans noktası)

// Z Ekseni (Dikey – Yukarı/Aşağı)
#define Z_STEP_PIN      6 // Step pini
#define Z_DIR_PIN       7 // Yön pini
#define Z_ENABLE_PIN    8 // Enable pini
#define Z_LIMIT_PIN     9   // Alt uç limit switch (Z = 0 referans noktası)

// Y Ekseni (Shuttle – İleri/Geri)
#define Y_STEP_PIN      10 // Step pini
#define Y_DIR_PIN       11 // Yön pini
#define Y_ENABLE_PIN    12  // Limit switch YOK – yazılım mesafesi ile kontrol

// ─── MOTOR PARAMETRELERİ ──────────────────────────────────────────────────────

#define STEPS_PER_REV       200      // 1.8° adım açısı → 200 adım/tur
#define MICROSTEP_FACTOR    16       // A4988 1/16 mikro adım
#define PULLEY_TEETH        20       // Kasnak diş sayısı
#define BELT_PITCH_MM       2.0f     // GT2 kayış diş aralığı (mm)

// Teorik değer: (200 * 16) / (20 * 2.0) = 80 adım/mm
#define STEPS_PER_MM        80.0f

// ─── HIZ AYARLARI (microsaniye cinsinden adımlar arası gecikme) ──────────────
//
// Formül: delay_us = 1_000_000 / (hız_mm_s * STEPS_PER_MM)
//   - 5 mm/s  → 1.000.000 / (5  * 80) = 2500 µs
//   - 3 mm/s  → 1.000.000 / (3  * 80) = 4167 µs (ana hareket hızı)
//   - 2 mm/s  → 1.000.000 / (2  * 80) = 6250 µs
//   - 1 mm/s  → 1.000.000 / (1  * 80) = 12500 µs (homing hızı)

#define SPEED_NORMAL_US     4167     // Normal hareket: ~3 mm/s
#define SPEED_SLOW_US       6250     // Yavaş hareket:  ~2 mm/s
#define SPEED_HOMING_US     12500    // Homing hızı:    ~1 mm/s

// ─── EKSENLERİN FİZİKSEL SINIR DEĞERLERİ (mm) ───────────────────────────────

#define X_MAX_MM            1300.0f  // X ekseni maksimum mesafe
#define Z_MAX_MM            1000.0f  // Z ekseni maksimum mesafe
#define Y_TRAVEL_MM         160.0f   // Y ekseni tek yön hareket mesafesi

// ─── RAF KONUMLARİ ────────────────────────────────────────────────────────────
//
// X Ekseni: X=0 sağda (giriş/çıkış noktası), solda raf sütunları
//   Sütun aralığı: 160 mm
//   Sütun 1 → 160 mm, Sütun 2 → 320 mm, Sütun 3 → 480 mm, Sütun 4 → 640 mm
//
// Z Ekseni: Z=0 altta, raf katları aşağıdan yukarıya
//   Kat 1 → 250 mm, Kat 2 → 500 mm, Kat 3 → 750 mm

#define SHELF_COLS          4
#define SHELF_ROWS          3

// Yatay sütun konumları (mm) – indeks 0'dan başlar
static const float SHELF_X_POS[SHELF_COLS] = {
    160.0f,   // Sütun 1
    320.0f,   // Sütun 2
    480.0f,   // Sütun 3
    640.0f    // Sütun 4
};

// Dikey kat konumları (mm) – indeks 0'dan başlar
static const float SHELF_Z_POS[SHELF_ROWS] = {
    250.0f,   // Kat 1 (en alt)
    500.0f,   // Kat 2 (orta)
    750.0f    // Kat 3 (en üst)
};

// ─── BIRAKIM/ALMA HAREKETİ (mm) ───────────────────────────────────────────────
// Paketi rafa bırakırken / raftan alırken Z ekseninde yapılan kısa hareket
#define Z_DROP_LIFT_MM      15.0f

// ─── SERİ HABERLEŞME ─────────────────────────────────────────────────────────
#define SERIAL_BAUD_RATE    9600

// ─── YÖN SABİTLERİ ───────────────────────────────────────────────────────────
#define DIR_POSITIVE        HIGH   // Eksende pozitif yön (X: sol, Z: yukarı, Y: ileri)
#define DIR_NEGATIVE        LOW    // Eksende negatif yön (X: sağ, Z: aşağı, Y: geri)

// ─── MOTOR ENABLE ─────────────────────────────────────────────────────────────
#define MOTOR_ENABLE        LOW    // A4988: LOW = etkin
#define MOTOR_DISABLE       HIGH   // A4988: HIGH = devre dışı

// ─── LIMIT SWITCH MANTIGI ─────────────────────────────────────────────────────
// Normalde açık (NO) tip switch, pull-up direnci ile bağlı
// Tetiklendiğinde pin LOW olur
#define LIMIT_TRIGGERED     LOW
#define LIMIT_FREE          HIGH

#endif // CONFIG_H
