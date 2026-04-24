/**
 * config.h
 * --------
 * Tüm sabit değerler, pin tanımları, motor parametreleri ve
 * raf konumları bu dosyada merkezi olarak tanımlanmıştır.
 * Hesaplama:
 *   Adım/mm = (200 adım/tur × 16 mikro adım) / 40 mm = 80 adım/mm
 */

#ifndef CONFIG_H
# define CONFIG_H

# define X_STEP_PIN 54  // RAMPS X_STEP
# define X_DIR_PIN 55   // RAMPS X_DIR
# define X_ENABLE_PIN 38 // RAMPS X_ENABLE
# define X_LIMIT_PIN 3  // RAMPS X- (X_MIN)

# define Z_STEP_PIN 36  // RAMPS E1_STEP (Z ekseni E1 slotuna taşındı)
# define Z_DIR_PIN 34   // RAMPS E1_DIR  (Z ekseni E1 slotuna taşındı)
# define Z_ENABLE_PIN 30 // RAMPS E1_ENABLE (Z ekseni E1 slotuna taşındı)
# define Z_LIMIT_PIN 18 // RAMPS Z- (Z_MIN)

# define Y_STEP_PIN 60   // RAMPS Y_STEP
# define Y_DIR_PIN 61    // RAMPS Y_DIR
# define Y_ENABLE_PIN 56 // RAMPS Y_ENABLE (Limit switch YOK)

# define STEPS_PER_REV 200   // 1.8° adım açısı → 200 adım/tur
# define MICROSTEP_FACTOR 32 // DRV8825 1/32 mikro adım
# define PULLEY_TEETH 20     // Kasnak diş sayısı
# define BELT_PITCH_MM 2.0f  // GT2 kayış diş aralığı (mm)

// Teorik değer: (200 * 32) / (20 * 2.0) = 160 adım/mm
# define STEPS_PER_MM 160.0f

// HIZ AYARLARI (microsaniye cinsinden adımlar arası gecikme)
//
// Formül: delay_us = 1_000_000 / (hız_mm_s * STEPS_PER_MM)
//   - 5 mm/s  → 1.000.000 / (5  * 160) = 1250 µs
//   - 3 mm/s  → 1.000.000 / (3  * 160) = 2083 µs (ana hareket hızı)
//   - 2 mm/s  → 1.000.000 / (2  * 160) = 3125 µs
//   - 1 mm/s  → 1.000.000 / (1  * 160) = 6250 µs (homing hızı)

# define SPEED_NORMAL_US 2083  // Normal hareket: ~3 mm/s
# define SPEED_SLOW_US 3125    // Yavaş hareket:  ~2 mm/s
# define SPEED_HOMING_US 6250  // Homing hızı:    ~1 mm/s

// EKSENLERİN FİZİKSEL SINIR DEĞERLERİ (mm)

# define X_MAX_MM 1300.0f   // X ekseni maksimum mesafe
# define Z_MAX_MM 1000.0f   // Z ekseni maksimum mesafe
# define Y_TRAVEL_MM 50.0f // Y ekseni tek yön hareket mesafesi (5 cm)

// RAF KONUMLARİ
# define SHELF_COLS 4
# define SHELF_ROWS 3

// Yatay sütun konumları (mm) – indeks 0'dan başlar
static const float	SHELF_X_POS[SHELF_COLS] = {
	160.0f, // Sütun 1
	320.0f, // Sütun 2
	480.0f, // Sütun 3
	640.0f  // Sütun 4
};

// Dikey kat konumları (mm) – indeks 0'dan başlar
static const float	SHELF_Z_POS[SHELF_ROWS] = {
	250.0f, // Kat 1 (en alt)
	500.0f, // Kat 2 (orta)
	750.0f  // Kat 3 (en üst)
};

// BIRAKIM/ALMA HAREKETİ (mm)
// Paketi rafa bırakırken / raftan alırken Z ekseninde yapılan kısa hareket
# define Z_DROP_LIFT_MM 15.0f
// Raf içine girmeden önce hedef kata yaklaşma ofseti (2-3 cm öneri)
# define Z_APPROACH_OFFSET_MM 25.0f
// Giris/teslim bolgesi icin hedef Z seviyeleri (kalibrasyonla guncellenmeli)
# define ENTRY_PICK_TARGET_Z_MM 250.0f
# define EXIT_DROP_TARGET_Z_MM 250.0f

// SERİ HABERLEŞME
# define SERIAL_BAUD_RATE 9600

// YÖN SABİTLERİ
# define DIR_POSITIVE HIGH // Eksende pozitif yön (X: sol, Z: yukarı, Y: ileri)
# define DIR_NEGATIVE LOW  // Eksende negatif yön (X: sağ, Z: aşağı, Y: geri)

// MOTOR ENABLE
# define MOTOR_ENABLE LOW   // A4988: LOW = etkin
# define MOTOR_DISABLE HIGH // A4988: HIGH = devre dışı

// LIMIT SWITCH MANTIGI
# define LIMIT_TRIGGERED LOW
# define LIMIT_NOT_TRIGGERED HIGH

#endif // CONFIG_H
