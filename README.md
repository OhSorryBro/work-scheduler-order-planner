# MIRO Order Planner (C#)
**Author:** Michał Domagała  
**Version:** 1.18  
**Status:** [ON HOLD]  
**Tech stack:** C#, .NET, RestSharp, Microsoft.Extensions.Configuration

## Project Description
A C# application for automatic planning and allocation of order slots to workstations and ready locations, with live visualization on a MIRO board via API.

The project was built for a real-world logistics use case: optimizing and reporting order preparation and handover processes in a warehouse environment.

---

## What does the application do?
- Takes input parameters: number of workstations (Formeren Stations), ready locations, order types/quantities, and scenario selection.
- Automatically plans the allocation of orders to stations, considering time constraints, order types, potential slot conflicts, severity levels, and slot availability.
- Integrates with MIRO: draws shapes on a MIRO board to represent tasks/orders, including colors and positions (via REST API).
- Reporting: generates error logs and CSV summary files (errors, order counts, etc.).
- Code tampering protection: simple anti-manipulation logic to detect unauthorized changes.

---

## Key Features
- Modular architecture (separate classes for stations, locations, orders, and core planning logic)
- Exception handling & multi-attempt planning in case of allocation errors
- Deep copy of planning structures (safe simulation of different layouts/scenarios)
- Async HTTP requests to the MIRO API
- CSV report export
- Easy to extend (add new order types, planning rules, locations, etc.)

---

## How to Run

1. **Clone the repository:**

2. **Add your MIRO API key to the appsettings.json file:**
    ```json
    {
      "Miro": {
        "ApiKey": "your_api_key_here"
      }
    }
    ```

3. **Create the CSV file for ready location statuses:**
   - The application requires a CSV file named `ready_locations_status.csv` in the working directory.
   - This file defines which ready locations are initially occupied (blocked) and for how long.
   - **Example content:**
     ```
     LocationId,Status,OccupiedUntil
     1,Occupied,0
     2,Occupied,480
     3,Occupied,0
     4,Occupied,0
     5,Occupied,480
     6,Occupied,0
     ```
   - `LocationId` — the ID of the ready location (should match IDs generated in the app, usually starting from 1).
   - `Status` — set to `Occupied` for blocked locations, otherwise use `Occupied,0`.
   - `OccupiedUntil` — number of minutes (from start of day) for which the location is blocked.

4. **Run the application:**
    - Visual Studio/VS Code: `F5` or `dotnet run`
    - Command line:
      ```
      dotnet run
      ```

5. **Follow the console instructions** to enter input parameters.  
   *Watch out! Some settings are set-up in code.*

---

## Requirements
- .NET 7.0 or higher
- RestSharp
- Microsoft.Extensions.Configuration

---

## Example Workflow
- User enters the number of stations and locations, and selects a scenario.
- Inputs the number of orders per type.
- The system automatically plans the allocation.
- Results are drawn on the MIRO board (each order as a separate shape).
- Logs and reports are saved as CSV files.

---

## Example screenshot

<img width="3328" height="1382" alt="image" src="https://github.com/user-attachments/assets/80c2d178-228e-437c-980a-dd7c915ba038" />

---

## Roadmap
Project is currently on hold (paused to focus on Data Analysis & BI career path).  
Future development may continue after gaining further experience in Python, BI, and analytics.

---

## Contact
**Michal Domagala**  
[LinkedIn](https://www.linkedin.com/in/michal-domagala-b0147b236/)

If you’d like to use this project or have questions, feel free to reach out via LinkedIn.

*If you’d like to use this project or have questions, feel free to reach out via LinkedIn.*
