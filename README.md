# DisplayApp

DisplayApp is a .NET application designed to simplify the process of managing display configurations when connecting to multiple external screens. If you frequently connect to different monitors and are tired of manually positioning them or setting the main display, this app provides a quick and automated solution.

## Features
- Automatically extends the desktop to all connected displays.
- Shows a number on each screen and lets you type the order from left to right — supports any number of displays.
- Bottom-aligns all screens to the primary display.
- Picks the primary display automatically: the **center** screen when there is an odd number of displays, otherwise the **largest** by pixel area.

## Usage
1. Download and build the project, or download the precompiled executable from the [Releases](https://github.com/nenning/DisplayApp/releases) page.
2. Open a terminal or command prompt and run the application:
   ```
   DisplayApp
   ```
3. The app extends the desktop and shows a number overlay on each screen.
4. Type the numbers from left to right, in the order the screens are physically arranged on your desk, and press Enter. Examples for three screens with display 2 on the left, 1 in the middle, and 3 on the right:
   - `2 1 3`
   - `2,1,3`
   - `213` (compact form, single-digit per screen — works for up to 9 displays)

## Prerequisites
- Windows operating system.
- .NET run-time
- At least two active displays.

## Building the Project
1. Clone the repository:
   ```
   git clone https://github.com/nenning/DisplayApp.git
   ```
2. Open the solution file in Visual Studio.
3. Build the project using the Release configuration.
4. The output executable will be located in the `bin\Release` folder.

## Contributing
Contributions are welcome! If you encounter issues or have feature requests, feel free to open an issue or submit a pull request.

## License
This project is licensed under the [MIT License](https://opensource.org/licenses/MIT). Feel free to use, modify, and distribute it.

## Acknowledgements
- Uses Windows API functions like `EnumDisplayDevices`, `ChangeDisplaySettingsEx`, and `SetDisplayConfig` for display management.
- Inspired by the need for a streamlined display configuration tool for multi-monitor setups.
