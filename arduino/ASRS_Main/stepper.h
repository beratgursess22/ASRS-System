/**
 * stepper.h
 * ---------
 * Düşük seviyeli step motor sürücü soyutlaması.
 * Her eksen için ayrı bir Stepper nesnesi oluşturulur.
 * Adım üretimi, yön kontrolü ve motor etkinleştirme/devre dışı bırakma
 * işlevleri bu modülde gerçekleştirilir.
 */

#ifndef STEPPER_H
#define STEPPER_H

#include <Arduino.h>
#include "config.h"

// ─── STEPPER YAPISI ──────────────────────────────────────────────────────────

struct StepperMotor {
    uint8_t stepPin;
    uint8_t dirPin;
    uint8_t enablePin;
    long    currentSteps;   // Referans noktasından itibaren toplam adım sayısı
    bool    enabled;
};

// ─── GLOBAL MOTOR NESNELERİ ──────────────────────────────────────────────────

extern StepperMotor motorX;
extern StepperMotor motorZ;
extern StepperMotor motorY;

// ─── FONKSİYON PROTOTİPLERİ ──────────────────────────────────────────────────

/**
 * @brief Verilen motoru başlatır; pinleri çıkış olarak ayarlar,
 *        motoru devre dışı bırakır ve adım sayacını sıfırlar.
 */
void stepperInit(StepperMotor &motor);

/**
 * @brief Tüm motorları başlatır (X, Y, Z).
 */
void allSteppersInit();

/**
 * @brief Motoru etkinleştirir (A4988 ENABLE pini LOW).
 */
void stepperEnable(StepperMotor &motor);

/**
 * @brief Motoru devre dışı bırakır (A4988 ENABLE pini HIGH).
 *        Taşıma tamamlandıktan sonra enerji tasarrufu için kullanılır.
 */
void stepperDisable(StepperMotor &motor);

/**
 * @brief Tüm motorları devre dışı bırakır.
 */
void allSteppersDisable();

/**
 * @brief Belirtilen yönde tek bir adım atar.
 * @param motor    Hareket ettirilecek motor
 * @param dir      DIR_POSITIVE veya DIR_NEGATIVE
 * @param delayUs  Adımlar arası gecikme (microsaniye)
 */
void stepperStep(StepperMotor &motor, uint8_t dir, unsigned int delayUs);

/**
 * @brief Belirtilen adım sayısı kadar hareket eder.
 * @param motor    Hareket ettirilecek motor
 * @param steps    Atılacak adım sayısı (mutlak değer)
 * @param dir      DIR_POSITIVE veya DIR_NEGATIVE
 * @param delayUs  Adımlar arası gecikme (microsaniye)
 */
void stepperMoveSteps(StepperMotor &motor, long steps, uint8_t dir, unsigned int delayUs);

/**
 * @brief mm cinsinden mesafeyi adım sayısına dönüştürür.
 * @param mm  Milimetre cinsinden mesafe
 * @return    Karşılık gelen adım sayısı
 */
long mmToSteps(float mm);

/**
 * @brief Adım sayısını mm cinsinden mesafeye dönüştürür.
 * @param steps  Adım sayısı
 * @return       Milimetre cinsinden mesafe
 */
float stepsToMm(long steps);

#endif // STEPPER_H
