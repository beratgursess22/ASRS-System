/**
 * axes.h
 * ------
 * X, Y ve Z eksenlerinin yüksek seviyeli kontrol arayüzü.
 * Homing (referanslama), mutlak konuma gitme ve göreceli hareket
 * işlevlerini kapsar.
 *
 * Koordinat sistemi:
 *   X = 0   → Sağ uç (Giriş/Çıkış noktası, limit switch tetiklenmiş)
 *   X artar → Sola hareket (raf sütunlarına doğru)
 *
 *   Z = 0   → Alt uç (limit switch tetiklenmiş)
 *   Z artar → Yukarı hareket (raf katlarına doğru)
 *
 *   Y = 0   → Geri konumu (shuttle içeride)
 *   Y artar → İleri hareket (rafa uzanma)
 */

#ifndef AXES_H
#define AXES_H

#include <Arduino.h>
#include "config.h"
#include "stepper.h"

// ─── FONKSİYON PROTOTİPLERİ ──────────────────────────────────────────────────

/**
 * @brief Limit switch pinlerini INPUT_PULLUP olarak başlatır.
 */
void axesInitLimitPins();

// ── Homing (Referanslama) ──────────────────────────────────────────────────

/**
 * @brief X eksenini sağa doğru hareket ettirerek limit switch'e kadar götürür.
 *        Limit switch tetiklendiğinde durur ve X = 0 olarak ayarlar.
 *        Sürücü başarısız olursa güvenli timeout ile durur.
 * @return true: başarılı, false: timeout nedeniyle başarısız
 */
bool homeX();

/**
 * @brief Z eksenini aşağı doğru hareket ettirerek limit switch'e kadar götürür.
 *        Limit switch tetiklendiğinde durur ve Z = 0 olarak ayarlar.
 * @return true: başarılı, false: timeout nedeniyle başarısız
 */
bool homeZ();

/**
 * @brief X ve Z eksenlerini sırasıyla referans noktalarına götürür.
 *        Sistem açıldığında veya her işlem döngüsünden önce çağrılır.
 *        Önce Z (güvenlik), sonra X homlanır.
 * @return true: her iki eksen de başarılı
 */
bool homeAll();

// ── Mutlak Konum Hareketi ─────────────────────────────────────────────────

/**
 * @brief X eksenini verilen mm konumuna götürür.
 *        Mevcut pozisyona göre yönü otomatik belirler.
 * @param targetMm  Hedef konum (mm, X=0 referansından)
 * @param delayUs   Adım gecikmesi (microsaniye)
 */
void moveXTo(float targetMm, unsigned int delayUs = SPEED_NORMAL_US);

/**
 * @brief Z eksenini verilen mm konumuna götürür.
 * @param targetMm  Hedef konum (mm, Z=0 referansından)
 * @param delayUs   Adım gecikmesi (microsaniye)
 */
void moveZTo(float targetMm, unsigned int delayUs = SPEED_NORMAL_US);

// ── Göreceli Y Hareketleri ────────────────────────────────────────────────

/**
 * @brief Y eksenini ileri yönde Y_TRAVEL_MM kadar hareket ettirir.
 *        (Rafa uzanma – paket bırakma/alma için)
 * @param delayUs Adım gecikmesi
 */
void moveYForward(unsigned int delayUs = SPEED_SLOW_US);

/**
 * @brief Y eksenini geri yönde Y_TRAVEL_MM kadar hareket ettirir.
 *        (Shuttle'ı geri çekme)
 * @param delayUs Adım gecikmesi
 */
void moveYBack(unsigned int delayUs = SPEED_SLOW_US);

// ── Kısa Z Hareketleri (Bırakma / Alma) ──────────────────────────────────

/**
 * @brief Paketin rafa bırakılması için Z ekseninde aşağıya kısa hareket yapar.
 *        Mesafe: Z_DROP_LIFT_MM
 */
void zDropDown();

/**
 * @brief Paketin raftan kaldırılması için Z ekseninde yukarıya kısa hareket yapar.
 *        Mesafe: Z_DROP_LIFT_MM
 */
void zLiftUp();

// ── Yardımcı ──────────────────────────────────────────────────────────────

/**
 * @brief Geçerli X konumunu mm cinsinden döndürür.
 */
float getCurrentX();

/**
 * @brief Geçerli Z konumunu mm cinsinden döndürür.
 */
float getCurrentZ();

#endif // AXES_H
