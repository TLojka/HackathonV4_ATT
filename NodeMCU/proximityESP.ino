#include <SoftwareSerial.h>
#include <CDBLEProx.h>

void ble_event(BLE_PROXIMITY_EVENT eventArgs);
SoftwareSerial sw(D5, D6);
CDBLEProximity ble(&sw, ble_event);

int major = 0;
int minor = 0;
int rssi = -1000;

void setup() {
  Serial.begin(57600);
  ble.begin();
}

void loop() {
  ble.update();
}

void ble_event(BLE_PROXIMITY_EVENT eventArgs) {
  if (eventArgs.eventID == BLE_EVENT_ON_DEVICE_LOST) {
    Serial.println("No device!");
    Serial.println("");
  }
  if (eventArgs.eventID == BLE_EVENT_ON_DEVICE_APPROACH) {
    major = eventArgs.device.hilo.substring(0, 4).toInt();
    minor = eventArgs.device.hilo.substring(4, 8).toInt();
    rssi = eventArgs.device.rssi;
    //    Serial.println("");
    //    Serial.println("New device");
    //    Serial.print("Major : "); Serial.println(major);
    //    Serial.print("Minor : "); Serial.println(minor);
    //    Serial.print("RSSI: "); Serial.println(rssi);
  }
  if (eventArgs.eventID == BLE_EVENT_ON_DEVICE_MOVED) {

    major = eventArgs.device.hilo.substring(0, 4).toInt();
    minor = eventArgs.device.hilo.substring(4, 8).toInt();
    rssi = eventArgs.device.rssi;
    //    Serial.println("");
    // .  Serial.println("Device moved");
    //    Serial.print("Major : "); Serial.println(major);
    //    Serial.print("Minor : "); Serial.println(minor);
    //    Serial.print("RSSI: "); Serial.println(rssi);
  }
  if (rssi > -75) {
    Serial.print("Pacient "); Serial.print(minor); Serial.println(" is in bed.");
  }
  if (rssi <= -75) {
    Serial.print("Pacient "); Serial.print(minor); Serial.println(" is not in bed!");
  }
}

