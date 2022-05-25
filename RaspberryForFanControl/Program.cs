using System.Device.Gpio;
namespace RaspberryForFanControl
{
    public class Program
    {
        private const string TEMP_PATH = @"/sys/class/thermal/thermal_zone0/temp";
        private const int GPIO_INDEX = 8;
        private const double TEMP_MAX = 60d;
        private static readonly ManualResetEvent _shutdownBlock = new(false);

        private static GpioController? _gpio;
        static void Main(string[] args)
        {
            Console.WriteLine($"{GetDate()}:RaspberryForFanControl start");
            _gpio = new(PinNumberingScheme.Board);
            _gpio.OpenPin(GPIO_INDEX, PinMode.Output, PinValue.High);
            //_gpio.SetPinMode(GPIO_INDEX, PinMode.Output);
            Console.WriteLine($"{GetDate()}:Open Gpio");
            Task.Run(async () =>
            {
                Console.WriteLine($"{GetDate()}:读取线程启动");

                while (true)
                {
                    if (File.Exists(TEMP_PATH))
                    {
                        using (FileStream fs = new FileStream(TEMP_PATH, FileMode.Open, FileAccess.Read))
                        {
                            using (StreamReader sr = new StreamReader(fs))
                            {
                                string? data = sr.ReadLine();
                                if (!string.IsNullOrEmpty(data))
                                {
                                    if (double.TryParse(data, out double temp))
                                    {
                                        temp = Math.Round(temp / 1000d, 2);
                                        Console.WriteLine($"{GetDate()}:CPU 温度:{temp}");
                                        if (temp > TEMP_MAX)
                                        {
                                            _gpio.Write(GPIO_INDEX, PinValue.High);
                                            Console.WriteLine($"{GetDate()}:开启风扇");
                                        }
                                        else
                                        {
                                            //关闭风扇
                                            _gpio.Write(GPIO_INDEX, PinValue.Low);
                                            Console.WriteLine($"{GetDate()}:关闭风扇");
                                        }
                                    }
                                    else
                                    {
                                        Console.Error.WriteLine($"{GetDate()}:温度转换失败!");
                                    }
                                }
                                else
                                {
                                    Console.Error.WriteLine($"{GetDate()}:温度读取为空!");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine($"{GetDate()}:文件不存在!");
                        break;
                    }
                    await Task.Delay(2000);
                }
            });

            Console.CancelKeyPress += OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += OnrocessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static string GetDate()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        private static void OnrocessExit(object? sender, EventArgs e)
        {
            _shutdownBlock.WaitOne();
            Console.WriteLine($"{GetDate()}:RaspberryForFanControl stop");
            _gpio?.ClosePin(GPIO_INDEX);
        }

        private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            Console.Error.Write(e.ToString());
        }

        private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _shutdownBlock.Set();
        }
    }
}