# DisplayApp

DisplayApp is a .NET application designed to simplify the process of managing display configurations when connecting your laptop to external screens. If you frequently connect to different monitors and are tired of manually positioning them or setting the main display, this app provides a quick and automated solution.

## Features
- Automatically sets the largest connected screen as the primary display.
- Positions your laptop display on the bottom-left or bottom-right of the main screen based on your preference.
- Simplifies screen management with a straightforward command-line interface.

## Usage
### Running the Application
1. Download and build the project, or download the precompiled executable from the [Releases](https://github.com/nenning/DisplayApp/releases) page.
2. Open a terminal or command prompt and run the application:
   ```
   DisplayApp -l
   ```
   **Options:**
   - `-l`: Positions the laptop display on the **left** of the primary screen.
   - `-r`: Positions the laptop display on the **right** of the primary screen.
   - If no parameters are provided, the app will prompt you to choose between `r` (right) or `l` (left) for the laptop screen position.

## Prerequisites
- Windows operating system.
- .NET run-time

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
- Uses Windows API functions like `EnumDisplayDevices` and `ChangeDisplaySettingsEx` for display management.
- Inspired by the need for a streamlined display configuration tool for multi-monitor setups.
