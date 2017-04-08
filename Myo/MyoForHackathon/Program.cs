using MyoSharp.Communication;
using MyoSharp.ConsoleSample.Internal;
using MyoSharp.Device;
using MyoSharp.Exceptions;
using MyoSharp.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATTM2X;
using ATTM2X.Classes;

namespace HackathonBrno
{
    internal class Program
    {   
        private static Boolean fallFlag = false,isAccelerometerSet= false, isPatientMoving = false,taskRunning=false;
        private static double criticalSpeed=1.4;
        private static int emgCounter = 0,fallCounter=0;
        private static List<List<int>> emgList = new List<List<int>>();
        private static List<List<double>> deltaList = new List<List<double>>();
        private static List<double> previous=new List<double>(),actual=new List<double>();
        private static QuaternionF centre = null;
        private static M2XClient m2x = new M2XClient("9ddb44e32cd115d5b5adc670f252a256");
        private static M2XResponse response;
        private static M2XStream movement,streamFall,patientState;
        private static int sendingCounter = 0;
        #region Methods
        private static void Main()
        {   
            //list initialisation
            for (int i = 0; i < 8; i++)
            {
                emgList.Add(new List<int>());
            }
            for (int i = 0; i < 3; i++)
            {
                deltaList.Add(new List<double>());
            }
            //M2X initialisation
            M2XDevice device = m2x.Device("7787671fb86b9e2bc3ccaa23ad934bf6");
            movement = device.Stream("patientMove");
            streamFall = device.Stream("hasFallen");
            patientState = device.Stream("patientState");
            // Myo initialisation from SDK
            using (var channel = Channel.Create(
                ChannelDriver.Create(ChannelBridge.Create(),
                MyoErrorHandlerDriver.Create(MyoErrorHandlerBridge.Create()))))
            using (var hub = Hub.Create(channel))
            {
                // listenin and register event handlers
                hub.MyoConnected += (sender, e) =>
                {
                    //e.Myo.Lock();
                    Console.WriteLine("Myo has been connected!", e.Myo.Handle);
                    //e.Myo.OrientationDataAcquired += Myo_OrientationDataAcquired;
                    e.Myo.AccelerometerDataAcquired += Myo_Accelerometer;
                    e.Myo.EmgDataAcquired += MyoEmgDataHandler;
                    e.Myo.SetEmgStreaming(true);
                    e.Myo.PoseChanged += poseHandler;
                    // e.Myo.Unlock(UnlockType.Hold);
                };

                // disabling myo listening and handlers
                hub.MyoDisconnected += (sender, e) =>
                {
                    Console.WriteLine("Myo was disconnected, data logging wont work.", e.Myo.Arm);
                    e.Myo.AccelerometerDataAcquired -= Myo_Accelerometer;
                    e.Myo.EmgDataAcquired -= MyoEmgDataHandler;
                    e.Myo.PoseChanged -= poseHandler;
                };

                // start listening for Myo data
                channel.StartListening();

                // wait on user input
                ConsoleHelper.UserInputLoop(hub);
            }
        }
        #endregion

        #region Event Handlers
        private static void Myo_Accelerometer(object sender, AccelerometerDataEventArgs e)
        { //This function monitors patients movement, writes if he is moving/falling

            //Console.Clear();
            //Console.WriteLine(e.Accelerometer.X + " " + e.Accelerometer.Y + " " + e.Accelerometer.Z);
            if (!isAccelerometerSet)
            {
                previous.Add(e.Accelerometer.X);
                previous.Add(e.Accelerometer.Y);
                previous.Add(e.Accelerometer.Z);
                isAccelerometerSet = true;
            }
            else
            {
                actual.Add(e.Accelerometer.X);
                actual.Add(e.Accelerometer.Y);
                actual.Add(e.Accelerometer.Z);
                for(int i = 0; i < 3; i++)
                {
                    if ((actual[i] > 0 && previous[i] > 0) || (actual[i] < 0 && previous[i] < 0))
                    {
                        var delta = Math.Abs(actual[i] - previous[i]);
                        deltaList[i].Add(delta);
                        if (criticalSpeed < delta && !fallFlag)
                        {
                            Console.WriteLine("Beware, patient may have fallen!.");
                            fallFlag = true;
                            response = streamFall.UpdateValue(new StreamValue { value = "fall" }).Result;
                        }
                    }
                    else
                    {
                        var delta = Math.Abs(actual[i] + previous[i]);
                        deltaList[i].Add(delta);
                        if (criticalSpeed < delta && !fallFlag)
                        { 
                            Console.WriteLine("Beware, patient may have fallen!.");
                            fallFlag = true;
                            response = streamFall.UpdateValue(new StreamValue { value = "fall" }).Result;

                        }

                    }
                   // Console.WriteLine(actual[i] + " " + previous[i] + " " + deltaList[i][deltaList[i].Count - 1]);
                }
                previous.Clear();
                previous.AddRange(actual);
                actual.Clear();
                if (deltaList[0].Count == 10)
                {
                   Boolean isPatientMovingFlag = false;
                    foreach(var list in deltaList)
                    {
                        var average = list.Average();
                        if (average>0.02) { isPatientMovingFlag = true; }
                        list.Clear();
                    }
                    if (isPatientMovingFlag) isPatientMoving = true;
                    else isPatientMoving = false;
                    Console.WriteLine("Is patient moving? " + isPatientMoving);
                    sendingCounter++;
                }
                if (sendingCounter==15)
                { int resultInt;
                    if (isPatientMoving) resultInt = 1; else resultInt = 0; 
                    response = movement.UpdateValue(new StreamValue { value = resultInt.ToString() }).Result;
                    sendingCounter = 0;
                }
            }
            if (fallFlag) fallCounter++;
            if (fallCounter == 200) { fallFlag = false; fallCounter = 0; }

        }
        private static void MyoEmgDataHandler(object sender, EmgDataEventArgs e)
        { //this function tells about actual activity of patient
            //communicates with M2X and surface in State of Patient table, where are 3 types of states
            //0 - low activity, possibly sleeping 1 - moderate activity 2 - high activity, patient may need some help
            for(int i = 0; i < 8; i++)
            {
                emgList[i].Add(Math.Abs(e.EmgData.GetDataForSensor(i)));
            }
            if (emgCounter == 1000 && !taskRunning)
            {
                taskRunning = true;
                emgCounter = 0;
                List<List<int>> emgTask = new List<List<int>>(emgList);
                var task = Task.Factory.StartNew(() => {
                    double averageCounter=0;
                    foreach(var list in emgTask)
                    {
                        averageCounter += list.Average();
                    }
                    if (averageCounter <= 10)
                    {
                        response = patientState.UpdateValue(new StreamValue { value = "0" }).Result;
                    }
                    if (averageCounter > 10 && averageCounter <= 70)
                    {
                        response = patientState.UpdateValue(new StreamValue { value = "1" }).Result;
                    }
                    if (averageCounter > 70)
                    {
                        response = patientState.UpdateValue(new StreamValue { value = "2" }).Result;
                    }
                    Console.WriteLine(averageCounter);
                    taskRunning = false;
                });
                foreach (var list in emgList)
                {
                    list.Clear();
                }
            }
            emgCounter++;

        }
        private static void poseHandler(object sender,PoseEventArgs e)
        {

        }

        #endregion
    }
}
