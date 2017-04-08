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
char stream2[] = "bedSensor2";                          //stream you want to push to
char key[] = "74f35b8acbb1f1482934f54056852d6e";      //your device API key

WiFiClient client;
M2XStreamClient m2xClient(&client, key);

void ble_event(BLE_PROXIMITY_EVENT eventArgs);
SoftwareSerial sw(D5, D6);
CDBLEProximity ble(&sw, ble_event);

int carerNotNearPatient = 0;
int patientNotInBed = 0;

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
  Serial.print("This is beacon finder in patient bed.");
  ble.begin();
}

void loop() {
  ble.update();
}

void ble_event(BLE_PROXIMITY_EVENT eventArgs) {
  if (eventArgs.eventID == BLE_EVENT_ON_DEVICE_LOST) {
    carerNotNearPatient++;
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
      carerNotNearPatient = 0;
      Serial.println("");
      Serial.print("One beacon, carer near patient "); Serial.println(carerNotNearPatient);
    }
    if (beacons.substring(0, 2) == "02" && beacons.substring(4, 6).toInt() > 75) {
      carerNotNearPatient++;
      Serial.println("");
      Serial.print("One beacon, carer NOT near patient "); Serial.println(carerNotNearPatient);
    }
    if (beacons.substring(0, 2) == "03"  && beacons.substring(4, 6).toInt() < 75) {
      patientNotInBed = 0;
      Serial.println("");
      Serial.print("One beacon, patient in the bed "); Serial.println(patientNotInBed);
    }
    if (beacons.substring(0, 2) == "03" && beacons.substring(4, 6).toInt() > 75) {
      patientNotInBed++;
      Serial.println("");
      Serial.print("One beacon, patient not in the bed "); Serial.println(patientNotInBed);
    }
  }
  if (beacons.length() == 14) {
    String beacon1 = beacons.substring(0, 6);
    String beacon2 = beacons.substring(7, 13);
    Serial.print("First: "); Serial.print(beacon1); Serial.print(", Second: "); Serial.println(beacon2);
    if (beacon1.substring(0, 2) == "02"  && beacon1.substring(4, 6).toInt() < 75) {
      carerNotNearPatient = 0;
      Serial.println("");
      Serial.print("Two beacons, carer near patient "); Serial.println(carerNotNearPatient);
    }
    if (beacon1.substring(0, 2) == "02" && beacon1.substring(4, 6).toInt() > 75) {
      carerNotNearPatient++;
      Serial.println("");
      Serial.print("Two beacons, carer NOT near patient "); Serial.println(carerNotNearPatient);
    }
    if (beacon2.substring(0, 2) == "02"  && beacon2.substring(4, 6).toInt() < 75) {
      carerNotNearPatient = 0;
      Serial.println("");
      Serial.print("Two beacons, carer near patient "); Serial.println(carerNotNearPatient);
    }
    if (beacon2.substring(0, 2) == "02" && beacon2.substring(4, 6).toInt() > 75) {
      carerNotNearPatient++;
      Serial.println("");
      Serial.print("Two beacons, carer NOT near patient "); Serial.println(carerNotNearPatient);
    }
    if (beacon1.substring(0, 2) == "03"  && beacon1.substring(4, 6).toInt() < 75) {
      patientNotInBed = 0;
      Serial.println("");
      Serial.print("Two beacons, carer near patient "); Serial.println(patientNotInBed);
    }
    if (beacon1.substring(0, 2) == "03" && beacon1.substring(4, 6).toInt() > 75) {
      patientNotInBed++;
      Serial.println("");
      Serial.print("Two beacons, carer NOT near patient "); Serial.println(patientNotInBed);
    }
    if (beacon2.substring(0, 2) == "03"  && beacon2.substring(4, 6).toInt() < 75) {
      patientNotInBed = 0;
      Serial.println("");
      Serial.print("Two beacons, carer near patient "); Serial.println(patientNotInBed);
    }
    if (beacon2.substring(0, 2) == "03" && beacon2.substring(4, 6).toInt() > 75) {
      patientNotInBed++;
      Serial.println("");
      Serial.print("Two beacons, carer NOT near patient "); Serial.println(patientNotInBed);
    }
  }

  if (carerNotNearPatient >= 5) {
    m2xClient.updateStreamValue(device, stream2, "0");
  }
  if (carerNotNearPatient < 5) {
    m2xClient.updateStreamValue(device, stream2, "1");
  }
}
