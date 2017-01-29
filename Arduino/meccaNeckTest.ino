#include <MeccaBrain.h>

MeccaBrain head(2);
MeccaBrain leftArm(3);
MeccaBrain leftArmMid(4);
MeccaBrain rightArm(5);


void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  Serial1.begin(9600);
}

void loop() {
  //put your main code here to run repeatedly
  //only loop 20 times per second
  delay(50);

  if (Serial.available() > 0) {
    String data = Serial.readStringUntil('\n');
    parseHeadData(data);
    parseLeftArmData(data);
  }
}

void parseHeadData(String data){
  int iStartH = data.indexOf("H");
  int iEndH = data.indexOf("/", iStartH);
  String sH0 = data.substring(iStartH+1, iEndH);
  int h0 = sH0.toInt();
  int h0Angle = (int)(h0/180.0*255.0);

  head.setServoPosition(1, h0Angle);
  head.communicate();
}

void parseLeftArmData(String data){
  int iStartL = data.indexOf("L");
  int iEndL = data.indexOf("/", iStartL);
  String sL0 = data.substring(iStartL+1, iEndL);
  int l0 = sL0.toInt();
  int l0Angle = (int)((1.0-(l0/180.0))*255.0);

  leftArmMid.setServoPosition(0, l0Angle);
  leftArmMid.communicate();
}

