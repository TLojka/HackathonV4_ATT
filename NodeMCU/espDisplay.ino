#include <LiquidCrystal_I2C.h>
LiquidCrystal_I2C lcd(0x27, 16, 2);

void setup() {
  lcd.begin(16,2);
  lcd.init();
  lcd.backlight();
  lcd.setCursor(0, 0);
  lcd.print("Monitoring...");
  lcd.setCursor(0, 1);      
  lcd.print("Temp:24, Hum:25");
}

void loop() {
}
