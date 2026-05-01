using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static NativeMethods;

class Program
{
    static Thread? overlayThread;
    static Form? overlayAnchor;

    static void Main()
    {
        int extendResult = SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero, SDC_TOPOLOGY_EXTEND | SDC_APPLY);
        if (extendResult != 0)
            Console.WriteLine($"SetDisplayConfig returned {extendResult}; continuing.");

        var displays = GetConnectedDisplaysWithRetry(minCount: 2);
        if (displays.Count < 2)
        {
            Console.WriteLine("At least two displays are required.");
            return;
        }

        ShowScreenNumberOverlays(displays);
        SetForegroundWindow(GetConsoleWindow());

        int[] order = ReadDisplayOrder(displays.Count);

        CloseOverlays();

        var ordered = order.Select(n => displays[n - 1]).ToList();
        PositionDisplays(ordered);
    }

    // --- Display enumeration ---

    static List<DEVMODE> GetConnectedDisplays()
    {
        var displays = new List<DEVMODE>();
        var device = new DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };

        for (uint id = 0; EnumDisplayDevices(null, id, ref device, 0); id++)
        {
            if (device.StateFlags.HasFlag(DisplayDeviceStateFlags.Active))
            {
                var devMode = new DEVMODE { dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)) };
                if (EnumDisplaySettings(device.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode))
                {
                    devMode.dmDeviceName = device.DeviceName;
                    displays.Add(devMode);
                    Console.WriteLine($"Active Display: {device.DeviceName} ({devMode.dmPelsWidth}x{devMode.dmPelsHeight})");
                }
            }
        }

        return displays;
    }

    static List<DEVMODE> GetConnectedDisplaysWithRetry(int minCount, int maxAttempts = 10, int delayMs = 500)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var displays = GetConnectedDisplays();
            // After SetDisplayConfig, EnumDisplaySettings may briefly report stale, overlapping origins.
            bool positionsDistinct = displays.Select(d => (d.dmPositionX, d.dmPositionY)).Distinct().Count() == displays.Count;
            if (displays.Count >= minCount && positionsDistinct)
                return displays;

            if (attempt < maxAttempts)
            {
                Console.WriteLine($"Waiting for display arrangement to settle... ({attempt}/{maxAttempts})");
                Thread.Sleep(delayMs);
            }
        }
        return GetConnectedDisplays();
    }

    // --- Overlay windows ---

    static void ShowScreenNumberOverlays(List<DEVMODE> displays)
    {
        var ready = new ManualResetEventSlim(false);

        overlayThread = new Thread(() =>
        {
            // PerMonitorV2 makes WinForms coordinates physical pixels — matches dmPositionX/dmPelsWidth.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            for (int i = 0; i < displays.Count; i++)
            {
                var d = displays[i];
                int overlayW = 200, overlayH = 200;
                int cx = d.dmPositionX + (int)d.dmPelsWidth / 2 - overlayW / 2;
                int cy = d.dmPositionY + (int)d.dmPelsHeight / 2 - overlayH / 2;

                var form = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.Manual,
                    TopMost = true,
                    BackColor = Color.Black,
                    Opacity = 0.8,
                    Size = new Size(overlayW, overlayH),
                    Location = new Point(cx, cy),
                    ShowInTaskbar = false
                };
                var label = new Label
                {
                    Text = (i + 1).ToString(),
                    Font = new Font("Arial", 80, FontStyle.Bold),
                    ForeColor = Color.White,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                form.Controls.Add(label);
                form.Show();

                if (i == 0) overlayAnchor = form;
            }

            // Signal ready only after the message pump has drained initial paints.
            EventHandler? onIdle = null;
            onIdle = (s, e) => { Application.Idle -= onIdle!; ready.Set(); };
            Application.Idle += onIdle;

            Application.Run();
        });

        overlayThread.SetApartmentState(ApartmentState.STA);
        overlayThread.IsBackground = true;
        overlayThread.Start();

        ready.Wait();
    }

    static void CloseOverlays()
    {
        overlayAnchor?.BeginInvoke(() => Application.Exit());
        overlayThread?.Join(2000);
    }

    // --- User input ---

    static int[] ReadDisplayOrder(int count)
    {
        while (true)
        {
            Console.Write($"Enter screen numbers from left to right (e.g. {string.Join(" ", Enumerable.Range(1, count))} or {string.Concat(Enumerable.Range(1, count))}): ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            int[] parsed;
            if (input.Contains(' ') || input.Contains(','))
            {
                var tokens = input.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (!TryParseInts(tokens, out parsed)) { PrintInvalid(count); continue; }
            }
            else
            {
                parsed = input.Select(c => c - '0').ToArray();
            }

            if (parsed.Length != count || parsed.Any(n => n < 1 || n > count) || parsed.Distinct().Count() != count)
            {
                PrintInvalid(count);
                continue;
            }

            return parsed;
        }
    }

    static bool TryParseInts(string[] tokens, out int[] result)
    {
        result = new int[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!int.TryParse(tokens[i], out result[i])) return false;
        }
        return true;
    }

    static void PrintInvalid(int count) =>
        Console.WriteLine($"Invalid input. Enter exactly {count} unique numbers between 1 and {count}.");

    // --- Display positioning ---

    static void PositionDisplays(List<DEVMODE> ordered)
    {
        // Primary = center display when count is odd, otherwise largest by pixel area
        int primaryIdx;
        if (ordered.Count % 2 == 1)
        {
            primaryIdx = ordered.Count / 2;
        }
        else
        {
            primaryIdx = 0;
            uint maxArea = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                uint area = ordered[i].dmPelsWidth * ordered[i].dmPelsHeight;
                if (area > maxArea) { maxArea = area; primaryIdx = i; }
            }
        }

        int primaryHeight = (int)ordered[primaryIdx].dmPelsHeight;

        // Calculate x positions with primary anchored at 0
        var xPositions = new int[ordered.Count];
        xPositions[primaryIdx] = 0;

        int x = 0;
        for (int i = primaryIdx - 1; i >= 0; i--)
        {
            x -= (int)ordered[i].dmPelsWidth;
            xPositions[i] = x;
        }
        x = (int)ordered[primaryIdx].dmPelsWidth;
        for (int i = primaryIdx + 1; i < ordered.Count; i++)
        {
            xPositions[i] = x;
            x += (int)ordered[i].dmPelsWidth;
        }

        // Apply: primary first (Windows requires primary at (0,0)), then all others
        var primary = ordered[primaryIdx];
        primary.dmPositionX = 0;
        primary.dmPositionY = 0;
        SetPrimaryDisplay(primary);

        for (int i = 0; i < ordered.Count; i++)
        {
            if (i == primaryIdx) continue;
            var d = ordered[i];
            d.dmPositionX = xPositions[i];
            d.dmPositionY = primaryHeight - (int)d.dmPelsHeight; // bottom-align to primary
            ApplyDisplaySettings(d);
        }

        ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        Console.WriteLine("Display configuration updated successfully.");
    }

    static void SetPrimaryDisplay(DEVMODE display)
    {
        display.dmFields |= DEVMODE.DM_POSITION;
        int result = ChangeDisplaySettingsEx(display.dmDeviceName, ref display, IntPtr.Zero,
            CDS_UPDATEREGISTRY | CDS_SET_PRIMARY | CDS_NORESET, IntPtr.Zero);

        if (result == DISP_CHANGE_SUCCESSFUL)
            Console.WriteLine($"Set {display.dmDeviceName} as primary.");
        else
            Console.WriteLine($"Failed to set {display.dmDeviceName} as primary. Error: {result}");
    }

    static void ApplyDisplaySettings(DEVMODE display)
    {
        display.dmFields |= DEVMODE.DM_POSITION;
        ChangeDisplaySettingsEx(display.dmDeviceName, ref display, IntPtr.Zero,
            CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero);
        Console.WriteLine($"Applied settings to {display.dmDeviceName}.");
    }
}
