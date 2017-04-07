#include <ESP8266WiFi.h>

void setup() {
  Serial.begin(115200);
  String clientMac = "";
  unsigned char mac[6];
  WiFi.macAddress(mac);
  clientMac += macToStr(mac);
  Serial.println();
  Serial.println(clientMac);
}

String macToStr(const uint8_t* mac){
 String result;
   for (int i = 0; i < 6; ++i) {
    result += String(mac[i], 16);
   if (i < 5)
    result += ':';
 }
 return result;
}

void loop() {

}
