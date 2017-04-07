#include <ESP8266WiFi.h>

#define ESP8266_PLATFORM
#include "M2XStreamClient.h"

int status = WL_IDLE_STATUS;

const char* ssid = "HackathonV4";                     //ssid of wifi network
const char* password = "hackhack";                    //password of wifi nwetwork
char device[] = "a77b817fbb437ad797bd23b428c47be3";   //ID of the Device you want to push to
char stream[] = "esp";                                //stream you want to push to
char key[] = "74f35b8acbb1f1482934f54056852d6e";      //your device API key

WiFiClient client;
M2XStreamClient m2xClient(&client, key);

void setup() {
 Serial.begin(9600);
 Serial.println();

 Serial.print("Connecting to "); Serial.print(ssid);
 WiFi.begin(ssid, password);
 while (WiFi.status() != WL_CONNECTED) {
 delay(500);
 Serial.print(".");
 } 
 Serial.println("");

 Serial.print("WiFi connected, IP address: "); Serial.println(WiFi.localIP());
}

void loop() {
  // number is the value you want to upload to m2x
  float number = float(random(0, 100));
  Serial.println(number);
  m2xClient.updateStreamValue(device, stream, number);

  delay(1000);                                          //delay
}
