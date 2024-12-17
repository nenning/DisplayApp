using System.Diagnostics;
using System.Runtime.InteropServices;

/* Top-left of main screen is always (0,0)

 * Laptop right: 
      +----------------------------------+ +--------------------+
      | Large Display                    | | Laptop Display     |
      | 3840x2160                        | | 1920x1080          |
      +----------------------------------+ +--------------------+
                                           ^
                                           |
                Horizontal Offset = 3840 --+
      
 * Laptop left:
      +--------------------+ +----------------------------------+
      | Laptop Display     | | Large Display                    |
      | 1920x1080          | | 3840x2160                        |
      +--------------------+ +----------------------------------+
      ^
      |
      +-- Offset = -1920

 */
class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)]
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        [MarshalAs(UnmanagedType.U4)]
        public DisplayDeviceStateFlags StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [Flags]
    public enum DisplayDeviceStateFlags : int
    {
        Active = 0x1,
        PrimaryDevice = 0x4,
        MirroringDriver = 0x8,
        VgaCompatible = 0x10,
        Removable = 0x20,
        ModesPruned = 0x8000000,
        Remote = 0x4000000,
        Disconnect = 0x2000000
    }
    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    public static extern bool EnumDisplaySettingsEx(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode, uint dwFlags);

    private static List<DEVMODE> GetConnectedDisplays()
    {
        var displays = new List<DEVMODE>();
        var device = new DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };

        for (uint id = 0; EnumDisplayDevices(null, id, ref device, 0); id++)
        {
            if (device.StateFlags.HasFlag(DisplayDeviceStateFlags.Active))
            {
                var devMode = new DEVMODE();
                devMode.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE));

                if (EnumDisplaySettings(device.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode))
                {
                    devMode.dmDeviceName = device.DeviceName; // Use the unique DeviceName
                    displays.Add(devMode);
                    Console.WriteLine($"Active Display: {device.DeviceName} ({devMode.dmPelsWidth}x{devMode.dmPelsHeight})");
                }
            }
        }

        return displays;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE
    {

        public const int DM_POSITION = 0x20;
        public const int DM_DISPLAYFLAGS = 0x2000000;
        public const int DM_PELSWIDTH = 0x80000;
        public const int DM_PELSHEIGHT = 0x100000;

        public const int CCHDEVICENAME = 32;
        public const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;

        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;

        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFrequency;
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }


    [DllImport("user32.dll")]
    public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

    [DllImport("user32.dll")]
    public static extern int ChangeDisplaySettingsEx(string deviceName, ref DEVMODE devMode, IntPtr hwnd, uint flags, IntPtr lParam);

    // workaround: see https://stackoverflow.com/a/23044185
    [DllImport("user32.dll")]
    public static extern int ChangeDisplaySettingsEx(string lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);


    const int ENUM_CURRENT_SETTINGS = -1;
    const int CDS_UPDATEREGISTRY = 0x01;

    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Write("Press l (laptop is on left) or r (laptop is on right): ");
            string arg = Console.ReadKey().KeyChar.ToString();
            Console.WriteLine();
            args = [ "-" + arg];
        }
        if (args.Length != 1 || (args[0] != "-l" && args[0] != "-r"))
        {
            Console.WriteLine("Usage: DisplayApp -l | -r");
            Console.WriteLine("-l: Laptop is on the left");
            Console.WriteLine("-r: Laptop is on the right");
            return;
        }

        bool laptopOnLeft = args[0] == "-l";
        ConfigureDisplays(laptopOnLeft);
        ExtendDisplays();
    }
    private static void ExtendDisplays()
    {
        try
        {
            // hacky way to extend the display. good enough for the current purpose.
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "DisplaySwitch.exe",
                    Arguments = "/extend",         
                    UseShellExecute = false,       
                    CreateNoWindow = true          
                }
            };
            process.Start();
            process.WaitForExit();            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to extend the display: {ex.Message}");
        }
    }

    private static void ConfigureDisplays(bool laptopOnLeft)
    {
        // Get all connected displays and their resolutions
        var displays = GetConnectedDisplays();
        if (displays.Count < 2)
        {
            Console.WriteLine("At least two displays are required.");
            return;
        }

        var largestDisplay = displays.OrderByDescending(d => d.dmPelsWidth * d.dmPelsHeight).First();
        var smallestDisplay = displays.First(d => d.dmDeviceName != largestDisplay.dmDeviceName);

        // Position the displays
        largestDisplay.dmPositionX = 0;
        largestDisplay.dmPositionY = 0;
        smallestDisplay.dmPositionY = (int)largestDisplay.dmPelsHeight - (int)smallestDisplay.dmPelsHeight; // Bottom of large display

        if (laptopOnLeft)
        {
            smallestDisplay.dmPositionX = -(int)smallestDisplay.dmPelsWidth;
        }
        else
        {
            smallestDisplay.dmPositionX = (int)largestDisplay.dmPelsWidth;
        }

        SetPrimaryDisplay(largestDisplay);
        ApplyDisplaySettings(smallestDisplay);
        ChangeDisplaySettingsEx(null, IntPtr.Zero, (IntPtr)null, 0, (IntPtr)null);
        Console.WriteLine("Display configuration updated successfully.");
    }
    private const int DISP_CHANGE_SUCCESSFUL = 0;
    public const int CDS_SET_PRIMARY = 0x10;
    public const int CDS_NORESET = 0x10000000;
    private static void SetPrimaryDisplay(DEVMODE display)
    {
        // Set the display as the primary display
        display.dmFields |= DEVMODE.DM_POSITION | DEVMODE.DM_DISPLAYFLAGS; ;
        display.dmPositionX = 0;
        display.dmPositionY = 0;

        // Apply changes
        int result = ChangeDisplaySettingsEx(display.dmDeviceName, ref display, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_SET_PRIMARY | CDS_NORESET, IntPtr.Zero);

        if (result == DISP_CHANGE_SUCCESSFUL)
        {
            Console.WriteLine($"Successfully set {display.dmDeviceName} as main display.");
        }
        else
        {
            Console.WriteLine($"Failed to set {display.dmDeviceName} settings. Error code: {result}");
        }
    }


    private static void ApplyDisplaySettings(DEVMODE display)
    {
        display.dmFields |= DEVMODE.DM_POSITION;
        ChangeDisplaySettingsEx(display.dmDeviceName, ref display, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
        Console.WriteLine($"Applied settings to {display.dmDeviceName}.");
    }
}
