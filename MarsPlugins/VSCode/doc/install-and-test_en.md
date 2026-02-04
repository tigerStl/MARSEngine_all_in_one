# How to Install and Test the Extension in VS Code

## Option 1: Development Debug (Recommended First)

Run the extension from the current project using the Extension Development Host. After changing code, run again to verify.

1. **Open the extension project in VS Code**
   - Menu: File → Open Folder → Select the repo root (the folder containing `package.json`)

2. **Install dependencies and compile**
   ```bash
   npm install
   npm run compile
   ```

3. **Start the Extension Development Host**
   - Press **F5**, or menu: Run → Start Debugging
   - A new VS Code window titled **[Extension Development Host]** will open

4. **Test the extension in the new window**
   - Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS) to open the Command Palette
   - Type `Java UI`; you should see:
     - **Java UI: Scan Application UI Elements**
     - **Java UI: Generate Test Script**
     - **Java UI: Select Java Process and Scan**
   - Run any of these to test the extension

5. **After changing code**
   - In the Extension Development Host window, press `Ctrl+R` (or `Cmd+R`) to reload the window, or close it and press F5 again

---

## Option 2: Package as .vsix and Install in VS Code

Use this to install on another machine or for regular use in your main VS Code instance.

1. **Install the packaging tool**
   ```bash
   npm install -g @vscode/vsce
   ```

2. **Package from the project root**
   ```bash
   cd <extension-project-root>
   npm run compile
   vsce package
   ```
   - This produces `java-ui-automation-0.1.0.vsix`

3. **Install the .vsix in VS Code**
   - Open VS Code (normal window, not Extension Development Host)
   - `Ctrl+Shift+P` → run **Extensions: Install from VSIX...**
   - Select the generated `java-ui-automation-0.1.0.vsix`
   - Reload the window when prompted

4. **Verify**
   - Open the Command Palette and type `Java UI`; the three commands above should appear

---

## Troubleshooting

- **“Java UI” commands do not appear**  
  Make sure `npm run compile` completed without errors. When using F5, run the commands in the **[Extension Development Host]** window.

- **“Agent JARs not found”**  
  Build the Java projects first: run `npm run build:java` from the repo root (or `mvn clean package` in the `java` directory).

- **“Failed to get Java processes”**  
  Build ProcessInfo: run `dotnet publish -c Release` in the `ProcessInfo` directory, or use `npm run build:processinfo`.

- **No log after clicking Window Spy**  
  - Open the Output panel, select "Java UI Automation", and check for errors
  - Confirm ProcessInfo is built: verify `ProcessInfo/bin/Release/net8.0/ProcessInfo.exe` exists
  - On first open, the panel should show "Java UI Automation panel ready..."; if not, the panel may not have loaded correctly

- **OTLPExporterError: Bad Request**  
  This is a Cursor telemetry error, unrelated to this extension. It can be ignored, or disable telemetry in Cursor settings.
