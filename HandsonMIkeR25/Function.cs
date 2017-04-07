﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tamir.SharpSsh;

namespace HandsonMIkeR25
{
    class Function
    {
       private static SshExec exec = new SshExec("192.168.3.21", "variscite"); // pripojenie robota Hanson n1 ROBOT A
       
        public static void SentSad(States state)
        {

            string outs = "";
                   
            try { 
            if (!exec.Connected)
                {
                    exec.Password = "passowrd";
                    exec.Connect();
                }

                if (state == States.cold)
            {
                outs = exec.RunCommand("python def.py");
                outs += exec.RunCommand("python bad.py");
            }
            if (state == States.warm)
            {
                outs = exec.RunCommand("python def.py");
                outs += exec.RunCommand("python fine.py");
            }
            if (state == States.confident)
            {
                outs = exec.RunCommand("python happy.py");
                outs += exec.RunCommand("python fine.py");
            }
            if (state == States.OK)
            {
                outs = exec.RunCommand("python happy.py");
                outs += exec.RunCommand("python fine.py");
            }
            if (state == States.bad)
            {
                outs = exec.RunCommand("python happy.py");
                outs += exec.RunCommand("python bad.py");
            }
            System.Console.WriteLine(outs);

             if (state == States.ASK)
             {
                 outs += exec.RunCommand("python bad.py");
             } 
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR WITH SSH " + e.Message);
            }
        }

    }

    public enum States
    {
        sad,
        happy,
        confident,
        pain,
        OK,
        warm,
        cold,
        bad,
        ASK
    }
}
