Welcome to the Best SocketIO Documentation! Best SocketIO is a leading Unity networking library engineered for a smooth integration of the Socket.IO technology. 
Ideal for dynamic real-time experiences like online multiplayer games, chat systems, and interactive dashboards.

Warning! **Dependency Alert**

    Best SocketIO depends on both the **Best HTTP** and **Best WebSockets** packages! 
    Ensure you've installed and configured both these packages in your Unity project before venturing into Best SocketIO. 
    Explore more about the [installation of Best HTTP](https://bestdocshub.pages.dev/HTTP/installation.md) and [Best WebSockets](https://bestdocshub.pages.dev/WebSockets/installation.md).

## Overview
In today's interconnected world, maintaining real-time interactions is pivotal for a vast array of applications. 
From multiplayer game lobbies to instant chat rooms, Socket.IO empowers these interactions with speed and efficiency. 
Best SocketIO makes it a breeze to incorporate this tech into your Unity endeavours, ensuring dynamic bi-directional exchanges.

## Key Features
- **Supported Unity Versions:** Best SocketIO is harmonized with Unity versions starting from :fontawesome-brands-unity: **2021.1 onwards**.
- **Compatibility with Socket.io:** Best SocketIO is fully compatible with the latest version of socket.io, ensuring you're equipped with cutting-edge real-time communication capabilities.
- **Cross-Platform Excellence:** Best SocketIO is designed for a broad range of Unity-supported platforms, making it versatile for an array of projects. It proudly supports:
    
    - :fontawesome-solid-desktop: **Desktop:** Windows, Linux, MacOS
    - :fontawesome-solid-mobile:  **Mobile:** iOS, Android
    - :material-microsoft-windows: **Universal Windows Platform (UWP)**
    - :material-web: **Web Browsers:** WebGL
    
    With such extensive platform support, Best SocketIO stands out as the go-to choice irrespective of your targeted platform or audience segment.

- **Seamless Integration:** With intuitive APIs and extensive documentation, integrating Best SocketIO into any Unity project is a straightforward process.
- **Performance Optimized:** Best SocketIO is engineered for high performance, ensuring minimal latency and efficient data transfers for real-time interactions.
- **Event-Driven Communication:** Embrace the power of event-based real-time communication, making your applications responsive and lively.
- **Auto-Reconnection:** Best SocketIO automatically manages reconnections, ensuring uninterrupted user experiences even in fluctuating network conditions.
- **Secure Layers:** Supporting encrypted connections, Best SocketIO ensures that your application's exchanges are secure and protected.
- **Profiler Integration:** Benefit from the in-depth [Best HTTP profiler](https://bestdocshub.pages.dev/Shared/profiler/index.md) integration:
    - **Memory Profiler:** Assess the internal memory allocations, optimizing performance and detecting potential memory issues.
    - **Network Profiler:** Monitor your networking intricacies, observing data exchanges, connection states, and more.
- **Extensible Rooms and Namespaces:** Organize your SocketIO communications with ease, creating distinct rooms[^1] and namespaces tailored to your app's needs.
- **Efficient Data Formats:** With support for both JSON and binary data, manage your data formats effectively.
- **Debugging and Logging:** Comprehensive logging options enable developers to get insights into the workings of the package and simplify the debugging process.

## Documentation Sections
Embark on your journey with Best SocketIO:

- [Installation Guide:](https://bestdocshub.pages.dev/SocketIO/installation.md) Begin with Best SocketIO by integrating the package and setting up your Unity ecosystem.
- [Upgrade Guide:](https://bestdocshub.pages.dev/SocketIO/upgrade-guide.md) Upgrading from an older variant? Get insights into the latest enhancements and streamline your upgrade process.
- [Getting Started:](https://bestdocshub.pages.dev/SocketIO/getting-started/index.md) Start your SocketIO adventure, learn the rudiments, and tailor Best SocketIO to your app's requirements.
- [Advanced Insights:](https://bestdocshub.pages.dev/SocketIO/intermediate-topics/index.md) Dive deeper with advanced SocketIO subjects, including event handling, rooms management, and more.

This comprehensive documentation caters to developers from various walks of life. 
Whether you're a Unity novice or a seasoned expert, these guides will bolster your journey, leveraging Best SocketIO's prowess.

Delve deep and supercharge your Unity creations with unmatched real-time interactions, all thanks to Best SocketIO!

## Installation Guide

!!! Warning "Dependency Alert"
    Before installing Best SocketIO, ensure you have the [Best HTTP package](../HTTP/index.md) and the [Best WebSockets package](../WebSockets/index.md) installed and set up in your Unity project. 
    If you haven't done so yet, refer to the [Best HTTP Installation Guide](../HTTP/installation.md) and the [Best WebSockets Installation Guide](../WebSockets/installation.md).

Getting started with Best SocketIO demands a prior setup of both the Best HTTP and Best WebSockets packages. After ensuring these are properly integrated, you can then effortlessly add Best SocketIO to your Unity projects.

### Installing from the Unity Asset Store using the Package Manager Window

1. **Purchase:** If you haven't previously purchased the package, proceed to do so. 
    Once purchased, Unity will recognize your purchase, and you can install the package directly from within the Unity Editor. If you already own the package, you can skip these steps.
    1. **Visit the Unity Asset Store:** Navigate to the [Unity Asset Store](https://assetstore.unity.com/publishers/4137?aid=1101lfX8E) using your web browser.
    2. **Search for Best SocketIO:** Locate and choose the official Best SocketIO package.
    3. **Buy Best SocketIO:** By clicking on the `Buy Now` button go though the purchase process.
2. **Open Unity & Access the Package Manager:** Start Unity and select your project. Head to [Window > Package Manager](https://docs.unity3d.com/Manual/upm-ui.html).
3. **Select 'My Assets':** In the Package Manager, switch to the [My Assets](https://docs.unity3d.com/Manual/upm-ui-import.html) tab to view all accessible assets.
4. **Find Best SocketIO and Download:** Scroll to find "Best SocketIO". Click to view its details. If it isn't downloaded, you'll notice a Download button. Click and wait. After downloading, this button will change to Import.
5. **Import the Package:** Once downloaded, click the Import button. Unity will display all Best SocketIO' assets. Ensure all are selected and click Import.
6. **Confirmation:** After the import, Best SocketIO will integrate into your project, signaling a successful installation.

### Installing from a .unitypackage file

If you have a .unitypackage file for Best SocketIO, follow these steps:

1. **Download the .unitypackage:** Make sure the Best SocketIO.unitypackage file is saved on your device. 
2. **Import into Unity:** Open Unity and your project. Go to Assets > Import Package > Custom Package.
3. **Locate and Select the .unitypackage:** Find where you saved the Best SocketIO.unitypackage file, select it, and click Open.
4. **Review and Import:** Unity will show a list of all the package's assets. Ensure all assets are selected and click Import.
5. **Confirmation:** Post import, you'll see all the Best SocketIO assets in your project's Asset folder, indicating a successful setup.

!!! Note
    Best SocketIO also supports other installation techniques as documented in Unity's manual for packages. 
    For more advanced installation methods, please see the Unity Manual on [Sharing Packages](https://docs.unity3d.com/Manual/cus-share.html).

### Assembly Definitions and Runtime References
For developers familiar with Unity's development patterns, it's essential to understand how Best SocketIO incorporates Unity's systems:

- **Assembly Definition Files:** Best SocketIO incorporates [Unity's Assembly Definition files](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html). It aids in organizing and managing the codebase efficiently.
- **Auto-Referencing of Runtime DLLs:** The runtime DLLs produced by Best SocketIO are [Auto Referenced](https://docs.unity3d.com/Manual/class-AssemblyDefinitionImporter.html), allowing Unity to automatically recognize and utilize them without manual intervention.
- **Manual Package Referencing:** Should you need to reference Best SocketIO manually in your project (for advanced setups or specific use cases), you can do so. 
Simply [reference the package](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html#reference-another-assembly) by searching for `com.Tivadar.Best.SocketIO`.

Congratulations! You've successfully integrated Best SocketIO into your Unity project. Begin your SocketIO adventure with the [Getting Started guide](getting-started/index.md).

For any issues or additional assistance, please consult the [Community and Support page](../Shared/support.md).