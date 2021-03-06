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
char device[] = "10aed5a52a0427c201de7c82f46b57fb";   //ID of the Device you want to push to
char stream[] = "carerRoom";                          //stream you want to push to
char key[] = "43767d8087571134a3d9e11092b55d0c";      //your device API key

WiFiClient client;
M2XStreamClient m2xClient(&client, key);

void ble_event(BLE_PROXIMITY_EVENT eventArgs);
SoftwareSerial sw(D5, D6);
CDBLEProximity ble(&sw, ble_event);

int carerInRoom = 0;

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
  Serial.print("This is beacon finder in carer room.");
  ble.begin();
}

void loop() {
  ble.update();
}

void ble_event(BLE_PROXIMITY_EVENT eventArgs) {
  if (eventArgs.eventID == BLE_EVENT_ON_DEVICE_LOST) {
    carerInRoom++;
  }
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

  String beacons = eventArgs.device.minorPlusRssi;
  if (beacons.length() == 7) {
    Serial.println("");
    beacons = beacons.substring(0, 6);
    Serial.print("First: "); Serial.println(beacons);
    Serial.print("Beacon: "); Serial.print(beacons.substring(0, 2)); Serial.print(", Signal: "); Serial.print(beacons.substring(4, 6));
    if (beacons.substring(0, 2) == "02"  && beacons.substring(4, 6).toInt() < 75) {
      carerInRoom = 0;
      Serial.println("");
      Serial.print("One beacon carer in the room "); Serial.println(carerInRoom);
    }
    if (beacons.substring(0, 2) == "02" && beacons.substring(4, 6).toInt() > 75) {
      carerInRoom++;
      Serial.println("");
      Serial.print("One beacon carer NOT in the room "); Serial.println(carerInRoom);
    }
  }
  if (beacons.length() == 14) {
    String beacon1 = beacons.substring(0, 6);
    String beacon2 = beacons.substring(7, 13);
    Serial.print("First: "); Serial.print(beacon1); Serial.print(", Second: "); Serial.println(beacon2);
    if (beacon1.substring(0, 2) == "02"  && beacon1.substring(4, 6).toInt() < 75) {
      carerInRoom = 0;
      Serial.println("");
      Serial.print("Two beacons, carer in the room "); Serial.println(carerInRoom);
    }
    if (beacon1.substring(0, 2) == "02" && beacon1.substring(4, 6).toInt() > 75) {
      carerInRoom++;
      Serial.println("");
      Serial.print("Two beacons, carer NOT in the room "); Serial.println(carerInRoom);
    }
    if (beacon2.substring(0, 2) == "02"  && beacon2.substring(4, 6).toInt() < 75) {
      carerInRoom = 0;
      Serial.println("");
      Serial.print("Two beacons, carer in the room "); Serial.println(carerInRoom);
    }
    if (beacon2.substring(0, 2) == "02" && beacon2.substring(4, 6).toInt() > 75) {
      carerInRoom++;
      Serial.println("");
      Serial.print("Two beacons, carer NOT in the room "); Serial.println(carerInRoom);
    }
  }

  if (carerInRoom >= 5) {
    m2xClient.updateStreamValue(device, stream, "0");
  }
  if (carerInRoom < 5) {
    m2xClient.updateStreamValue(device, stream, "1");
  }
}
