#include <ESP8266WiFi.h>
#include <SoftwareSerial.h>
#include <CDBLEProx.h>
#define ESP8266_PLATFORM
#include "M2XStreamClient.h"

int status = WL_IDLE_STATUS;
int major = 0;
int minor = 0;
int rssi = -1000;

const char* ssid = "HackathonV4";                     //ssid of wifi network
const char* password = "hackhack";                    //password of wifi nwetwork
char device[] = "a77b817fbb437ad797bd23b428c47be3";   //ID of the Device you want to push to
char stream[] = "bedSensor";                          //stream you want to push to
char key[] = "74f35b8acbb1f1482934f54056852d6e";      //your device API key

WiFiClient client;
M2XStreamClient m2xClient(&client, key);

void ble_event(BLE_PROXIMITY_EVENT eventArgs);
SoftwareSerial sw(D5, D6);
CDBLEProximity ble(&sw, ble_event);

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

  ble.begin();
}

void loop() {
  ble.update();
}

void ble_event(BLE_PROXIMITY_EVENT eventArgs) {
  //if (eventArgs.eventID == BLE_EVENT_ON_DEVICE_LOST) {
  //  Serial.println("No device...");
  //}
  if (eventArgs.eventID == BLE_EVENT_ON_DEVICE_APPROACH) {
    major = eventArgs.device.hilo.substring(0, 4).toInt();
    minor = eventArgs.device.hilo.substring(4, 8).toInt();
    rssi = eventArgs.device.rssi;
  }
  if (eventArgs.eventID == BLE_EVENT_ON_DEVICE_MOVED) {
    major = eventArgs.device.hilo.substring(0, 4).toInt();
    minor = eventArgs.device.hilo.substring(4, 8).toInt();
    rssi = eventArgs.device.rssi;

  }
  if (rssi > -75 && minor == 1) {
    Serial.println("Pacient is in the bed.");
    m2xClient.updateStreamValue(device, stream, "Pacient is in the bed.");
  }
  if (rssi <= -75 && minor == 1) {
    Serial.println("Pacient is not in the bed!");
    m2xClient.updateStreamValue(device, stream, "Pacient is not in the bed!");
  }
  if (rssi > -75 && minor == 2) {
    Serial.println("Carer is nearby.");
    m2xClient.updateStreamValue(device, stream, "Carer is nearby.");
  }
  if (rssi <= -75 && minor == 2) {
    Serial.println("Carer is not in room.");
    m2xClient.updateStreamValue(device, stream, "Carer is not in room.");
  }
}
