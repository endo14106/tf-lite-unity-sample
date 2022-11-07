using System.Data;
using Internal;
using System;
class TestClass
{
    static void Main(string[] args)
    {
        double cos1 = 0.90;
        double cos2 = 0.975;
        double th1 = 0.91;
        double th2 = 0.98;

        if(IniCheckTh(cos1, th1) == 1){
            Console.WriteLine("初期姿勢OK");
        }
        if(SinkCheckTh(cos2, th2) == 1){
            Console.WriteLine("最深姿勢OK");
        }
    }

    public static int IniCheckTh(double cos, double th)
    {
        if(cos <= th){
            return 1;
        }
        else{
            return 0;
        }
    }

    public static int SinkCheckTh(double cos, double th)
    {
        if(cos >= th){
            return 1;
        }
        else{
            return 0;
        }
    }
}