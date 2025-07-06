WorkSchedulerOrderPlanner
Console application for optimal scheduling of “Formeren” stations and “Ready” locations in the Heijen department.

Project Description
WorkSchedulerOrderPlanner is a C# console application designed to model and optimize the order preparation workflow in the Heijen department. The process consists of two main phases:
Formeren (Preparation): Collect and assemble materials into ready-for-zone orders. Each Formeren station represents a preparation zone with a fixed available time (e.g., 1440 minutes per day).
Ready Location (Awaiting Pickup): After processing, orders are moved to designated Ready locations where they wait for pickup, each with its own time capacity.

The application:
Prompts the user for the number of Formeren stations and Ready locations.
Collects the quantity of order slots per category (operation-specific and customizable as needed)
Generates lists of station and location objects with their available time in minutes.

Requirements

.NET Framework 4.8 (or higher) or .NET 6.0+ (if ported)
Visual Studio 2019/2022 or any C# IDE

Configuration and Run
Run the console application directly—no external configuration files are required; all settings are defined in the source code.
cd src/Planner.ConsoleApp
dotnet run --configuration Release
Follow the console prompts to enter the number of stations, locations, and order slots per category.

Example Usage
After running the app:

Welcome to WorkSchedulerOrderPlanner!
Please enter the number of Formeren stations available: 3
Please enter the number of Ready locations available: 2
Please enter the number of order slots for VE(A): 5
...
The app will then display the generated station and location objects and (in the future) export the plan to a file.


Contributing
Fork the repository.
Create a feature branch: feature/your-feature-name.
Implement your changes and add tests.
Submit a pull request.
