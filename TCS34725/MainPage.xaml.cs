using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TCS34725
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Constants borrowed from Adafruit's Python library:
        // https://github.com/adafruit/Adafruit-Raspberry-Pi-Python-Code/tree/master/Adafruit_TCS34725
        // See the datasheet for the details:
        // https://www.adafruit.com/datasheets/TCS34725.pdf

        const byte __TCS34725_ADDRESS       = 0x29;
        const byte __TCS34725_ID            = 0x12; // 0x44 = TCS34721/TCS34725, 0x4D = TCS34723/TCS34727
        const byte __TCS34725_COMMAND_BIT   = 0x80;
        const byte __TCS34725_CDATAL        = 0x14; // Clear channel data
        const byte __TCS34725_CDATAH        = 0x15;
        const byte __TCS34725_RDATAL        = 0x16; // Red channel data
        const byte __TCS34725_RDATAH        = 0x17;
        const byte __TCS34725_GDATAL        = 0x18; // Green channel data
        const byte __TCS34725_GDATAH        = 0x19;
        const byte __TCS34725_BDATAL        = 0x1A; // Blue channel data
        const byte __TCS34725_BDATAH        = 0x1B;
        const byte __TCS34725_ENABLE        = 0x00;
        const byte __TCS34725_ENABLE_AIEN   = 0x10; // RGBC Interrupt Enable
        const byte __TCS34725_ENABLE_WEN    = 0x08; // Wait enable - Writing 1 activates the wait timer
        const byte __TCS34725_ENABLE_AEN    = 0x02; // RGBC Enable - Writing 1 actives the ADC, 0 disables it
        const byte __TCS34725_ENABLE_PON    = 0x01; // Power on - Writing 1 activates the internal oscillator, 0 disables it
        const byte __TCS34725_CONTROL       = 0x0F; // Set the gain level for the sensor
        const byte __TCS34725_ATIME         = 0x01; // Integration time

        I2cDevice sensor;

        Windows.UI.Xaml.DispatcherTimer timer;

        public MainPage()
        {
            this.InitializeComponent();
            Init();
        }

        public async void Init()
        {
            await InitRGBSensor();

            timer = new Windows.UI.Xaml.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            int[] data = new int[4];

            ReadRGBData(data);
            Color c = ConvertToColor(data[0], data[1], data[2], data[3]);
            rectangle.Fill = new SolidColorBrush(c);

            textBlock.Text = "R: " + c.R + " G: " + c.G + " B : " + c.B;
        }

        public Color ConvertToColor(int r, int g, int b, int c)
        {
            double fr = r, fg = g, fb = b, fc = c;

            fr /= fc;fr *= 255;
            fg /= fc; fg *= 255; 
            fb /= fc; fb *= 255; 

            return Color.FromArgb(255, Convert.ToByte(fr), Convert.ToByte(fg), Convert.ToByte(fb));
        }

        public async Task InitRGBSensor()
        {
            var selector = I2cDevice.GetDeviceSelector();
            var devices = await DeviceInformation.FindAllAsync(selector);
            var settings = new I2cConnectionSettings(__TCS34725_ADDRESS);
            sensor = await I2cDevice.FromIdAsync(devices[0].Id, settings);

            // Make sure we are talking to a TCS34725

            byte[] write_buffer = new byte[] { __TCS34725_ID | __TCS34725_COMMAND_BIT };
            byte[] read_buffer = new byte[1];

            sensor.WriteRead(write_buffer, read_buffer);

            if (read_buffer[0] != 0x44)
            {
                throw new Exception("not connected to TCS34725");
            }

            // Send a few commands

            byte[] command_buffer = new byte[2];

            // Turn on

            command_buffer[0] = __TCS34725_ENABLE | __TCS34725_COMMAND_BIT;
            command_buffer[1] = __TCS34725_ENABLE_PON;
            sensor.Write(command_buffer);

            command_buffer[0] = __TCS34725_ENABLE | __TCS34725_COMMAND_BIT;
            command_buffer[1] = __TCS34725_ENABLE_PON | __TCS34725_ENABLE_AEN;
            sensor.Write(command_buffer);

            // Integration Time

            command_buffer[0] = __TCS34725_ATIME | __TCS34725_COMMAND_BIT;
            command_buffer[1] = 0x00;
            sensor.Write(command_buffer);

            // Gain

            command_buffer[0] = __TCS34725_CONTROL | __TCS34725_COMMAND_BIT;
            command_buffer[1] = 0x01;
            sensor.Write(command_buffer);
        }

        public void ReadRGBData(int[] RGBC)
        {
            byte[] write_buffer = new byte[1];
            byte[] read_buffer = new byte[1];

            int r, g, b, c;

            write_buffer[0] = __TCS34725_RDATAL | __TCS34725_COMMAND_BIT;
            sensor.WriteRead(write_buffer, read_buffer);
            r = read_buffer[0];
            write_buffer[0] = __TCS34725_RDATAH | __TCS34725_COMMAND_BIT;
            sensor.WriteRead(write_buffer, read_buffer);
            r += read_buffer[0] << 8;

            write_buffer[0] = __TCS34725_GDATAL | __TCS34725_COMMAND_BIT;
            sensor.WriteRead(write_buffer, read_buffer);
            g = read_buffer[0];
            write_buffer[0] = __TCS34725_GDATAH | __TCS34725_COMMAND_BIT;
            sensor.WriteRead(write_buffer, read_buffer);
            g += read_buffer[0] << 8;

            write_buffer[0] = __TCS34725_BDATAL | __TCS34725_COMMAND_BIT;
            sensor.WriteRead(write_buffer, read_buffer);
            b = read_buffer[0];
            write_buffer[0] = __TCS34725_BDATAH | __TCS34725_COMMAND_BIT;
            sensor.WriteRead(write_buffer, read_buffer);
            b += read_buffer[0] << 8;

            write_buffer[0] = __TCS34725_CDATAL | __TCS34725_COMMAND_BIT;
            sensor.WriteRead(write_buffer, read_buffer);
            c = read_buffer[0];
            write_buffer[0] = __TCS34725_CDATAH | __TCS34725_COMMAND_BIT;
            sensor.WriteRead(write_buffer, read_buffer);
            c += read_buffer[0] << 8;

            RGBC[0] = r;
            RGBC[1] = g;
            RGBC[2] = b;
            RGBC[3] = c;
        }
    }
}
