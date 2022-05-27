using System.Device.Gpio;
namespace RaspberryForDTH11 // Note: actual namespace depends on the project name.
{
    public class Program
    {
        private static readonly int PinIndex = 7;
        static GpioController? gpio;
        static int[] dht11_dat = { 0, 0, 0, 0, 0 };
        static void Main(string[] args)
        {

            Console.WriteLine("start");

            Task.Run(async () =>
            {
                gpio = new(PinNumberingScheme.Board);
                gpio.OpenPin(PinIndex);
                while (true)
                {
                    await Task.Delay(3000);
                    if (ReadData())
                    {
                        Console.WriteLine($"读取数据成功");
                        Console.WriteLine($" 湿度： {dht11_dat[0]}.{dht11_dat[1]}%   温度：{dht11_dat[2]}.{dht11_dat[3]}℃");
                    }
                }
            });
            Console.ReadKey();
            gpio?.ClosePin(PinIndex);
        }

        private static bool ReadData()
        {
            PinValue lastState = PinValue.High;
            int i, j = 0;
            dht11_dat[0] = dht11_dat[1] = dht11_dat[2] = dht11_dat[3] = dht11_dat[4] = 0;
            gpio?.SetPinMode(PinIndex, PinMode.Output);
            gpio?.Write(PinIndex, 0);
            Thread.Sleep(18);
            gpio?.Write(PinIndex, 1);
            WaitMicroseconds(40);
            gpio?.SetPinMode(PinIndex, PinMode.Input);

            for (i = 0; i < 85; i++)
            {
                int counter = 0;
                while (gpio?.Read(PinIndex) == lastState)
                {
                    counter++;
                    WaitMicroseconds(1);
                    if (counter == 255)
                    {
                        break;
                    }
                }
                lastState = gpio.Read(PinIndex);
                if (counter == 255)
                {
                    break;
                }
                if ((i >= 4) && (i % 2 == 0))
                {

                    dht11_dat[j / 8] <<= 1;
                    if (counter > 16)
                    {
                        dht11_dat[j / 8] |= 1;
                    }
                    j++;
                }
            }

            if ((j >= 40) && (dht11_dat[4] == ((dht11_dat[0] + dht11_dat[1] + dht11_dat[2] + dht11_dat[3]) & 0xFF)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void WaitMicroseconds(int microseconds)
        {
            var until = DateTime.UtcNow.Ticks + (microseconds * 10);
            while (DateTime.UtcNow.Ticks < until) { }
        }
    }
}