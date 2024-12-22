This project is still in development and the README.md is a work in progress!

# 🔧 FNAF Studio

![GitHub Repository Size](https://img.shields.io/github/repo-size/ghatt-o/FNAF-Studio?style=for-the-badge)
![GitHub Forks](https://img.shields.io/github/forks/ghatt-o/FNAF-Studio?style=for-the-badge)

> FNAF Studio is an easy and fast solution to your 2D FNAF Games. It allows you to create simple FNAF spin-offs all the
> way to fully fletched fangames with mobile ports. An upgrade to FNAF Maker.

### 🚀 Main features in development

- [X] Easy playtesting with the click of a button
- [ ] Export to EXE, APK and HTML5
- [x] Designed to edit from Windows and Linux
- [ ] FNAF World and 3D features
- [ ] Custom Shaders
- [ ] Minigames API
- [x] Customizable default images (like the power usage or static effects)

## 💻 Requirements

- Windows 10, 11 or any mainstream Linux distribution (recommended)
- 4GB RAM (recommended)
- 400MB storage (recommended)

## ⚙️ Building the Project

1. **Clone the Repository**  
   ```bash
   git clone https://github.com/ghatt-o/FNAF-Studio.git
   cd FNAF-Studio
   ```

2. **Install .NET SDK**  
   Ensure you have the [.NET SDK](https://dotnet.microsoft.com/download) installed (version 8.0 or later).

3. **Open the Solution**  (optional)
   Open the `FNaF Studio.sln` file in your preferred IDE (e.g., Visual Studio, JetBrains Rider, or Visual Studio Code).

4. **Build the Solution**  
   Build the solution using your IDE or from the terminal:  
   ```bash
   dotnet build --configuration Release
   ```

5. **Find the Output**  
   The built files will be in the `build` directory:  
   ```
   FNAF-Studio/build/Runtime/Release
   FNAF-Studio/build/Editor/Release
   ```

## 🛠️ Usage

1. **Launch the Editor**  
   After building, run the FNAF Studio Editor from the `build/Editor/Release` directory:  
   ```bash
   cd build/Editor/Release
   ./Editor.exe # Windows
   ./Editor # Linux
   ```

2. **Create Your Game**  
   - Use the editor to design your game.  
   - Playtest your game. 

3. **Export Your Game**  
   (Feature under development) Export your game to different platforms, such as Windows executables, Android APKs, or HTML5 builds.

## 📝 License

This project is under a specific [license](LICENSE.md).
