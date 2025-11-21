# Contract Monthly Claim System (CMCS)

PROG6212 – Part 3  

GitHub Repository link: https://github.com/Tema-Mamaphale/PROG6212_POEPART3
YouTube Video: https://youtu.be/82705mzC8hU


## 1. Main Idea of the System

The Contract Monthly Claim System (CMCS) is a web based ASP.NET Core MVC application that automates the monthly claim process for Independent Contractors (Lecturers).

Instead of using spreadsheets and email, the system provides:

- A single place for lecturers to submit their monthly claims.
- A clear approval workflow:
  - Lecturer (IC) → Programme Coordinator → Academic Manager → HR.
- A central place for HR to manage lecturer profiles and view approved claims summaries.

The goal is to reduce manual admin, standardise the claim process, and provide a clear audit trail of who approved what and when.

## 2. User Roles and What They Can Do

The system simulates login using a role selector (`Home/SelectRole`) and sessions.  
Each role only sees the pages that match its responsibilities.

### 2.1 Lecturer (Independent Contractor)

Purpose: Submit a monthly claim with hours, rate, and supporting documentation.

Main screens / actions:

Submit Monthly Claim – `Claims/Submit`
  - Select lecturer profile (from HR-managed list).
  - Month (e.g. “October 2025”).
  - Hours worked (with numeric validation and a monthly cap).
  - Hourly rate (pre-populated from HR and locked – cannot be edited by the lecturer).
  - Auto-calculated total (Hours × Rate) shown live on the screen.
  - Upload a supporting file:
    - Allowed: `.pdf`, `.docx`, `.xlsx`
    - Maximum size: 10 MB
  - Server-side validation to prevent:
    - Hours ≤ 0 or above a configured monthly maximum.
    - Hourly rate outside the allowed range.
    - Missing required fields.
    - Invalid or oversized files.

  Track Status – `Claims/StatusList` and `Claims/Status`
  - View the latest claims and their statuses:
    - `Submitted`
    - `PendingReview`
    - `Approved`
    - `Rejected`

### 2.2 Programme Coordinator

Purpose: First-level approval / quality check.

Main screens / actions:

Review Submitted Claims – Claims/Review / `Claims/CoordinatorReview`
  - Sees only claims with status Submitted.
  - Actions:
    - Forward to Academic Manager → status becomes `PendingReview`.
    - Reject the claim → status becomes `Rejected`.

- The controller uses a helper `TryTransition` to ensure the claim **can only move** from `Submitted` → `PendingReview` or `Rejected`.

### 2.3 Academic Manager

Purpose:Final approval decision before HR reporting.

Main screens / actions:

Manager Review – Claims/ManagerReview
  - Sees only claims with status PendingReview.
  - Actions:
    - Approve → status becomes Approved.
    - Reject  → status becomes Rejected.

- The same `TryTransition` helper prevents the manager from approving claims that did not come through the Programme Coordinator stage.

### 2.4 HR (Super User)

**Purpose:** Maintain lecturer profiles and use the approved claims for reporting.

Main screens / actions:

HR Dashboard – Monthly Summary – HR/Index
  - Select a month from a dropdown.
  - See one row per lecturer with:
    - Total hours for that month.
    - Average hourly rate.
    - Total amount due.
  - View grand totals across all lecturers.

  Per-Lecturer Invoice – HR/Invoice
  - Shows a formatted, print-ready invoice of all approved claims for a lecturer and month.
  - Can be printed and saved as PDF from the browser (“Print → Save as PDF”).

- CSV Export
  - Export approved claims for a selected month: HR/ExportApprovedForMonthCsv.
  - Export all approved claims: `Claims/ExportApprovedCsv`.

- Lecturer Management – `HRLecturers/Index`, `Create`, `Edit`
  - Add new lecturer profiles.
  - Edit name, contact details, department.
  - Maintain Hourly Rate (R).
  - Mark lecturers as Active or Inactive.
  - These profiles feed into the lecturer dropdown on the submission form, and the hourly rate value is pulled from here.

## 3. Technologies Used

- Backend Framework: ASP.NET Core MVC
- Language: C#
- Database: SQL Server (managed via SSMS)
- ORM: Entity Framework Core
- UI Framework: Bootstrap 5 (with Bootstrap Icons)
- Client-side validation: jQuery Validation + Unobtrusive validation
- State Management: ASP.NET Core Session (ISession) to store the selected role
- Views: Razor (.cshtml) with layout _Layout.cshtml
- Build & Run: Visual Studio / dotnet CLI


## 4. System Flow (IC → PC → AM → HR)

The approval flow is enforced in code using:

- A `ClaimStatus` enum:
  - `Draft`, `Submitted`, `PendingReview`, `Approved`, `Rejected`.
- A `TryTransition` helper inside `ClaimsController` that ensures:
  - Only claims in `Submitted` can go to `PendingReview` or `Rejected` by the Programme Coordinator.
  - Only claims in `PendingReview` can go to `Approved` or `Rejected` by the Academic Manager.
- Role-based filters `[RoleRequired(...)]` on controller actions so that:
  - Only Lecturer can access the submission screen.
  - Only Coordinator can access `CoordinatorReview`, `CoordinatorApprove`, `CoordinatorReject`.
  - Only Manager can access `ManagerReview`, `ManagerApprove`, `ManagerReject`.
  - Only HR can access `HR` and `HRLecturers` controllers.

Final view for HR is reporting only: HR sees approved claims and summaries but does not add or approve claims.


## 5. Demo Roles and Credentials (For Marking)

Role selection is simulated through the **Select Role** page, but the following demo credentials are used consistently in the UI and README as requested:

- **Lecturer (IC)**
  - Email: `lecturer@cmcs.test`
  - Password: `Lec@123`
  - Role: Lecturer – submits monthly claims with supporting documents.

- **Programme Coordinator**
  - Email: `coordinator@cmcs.test`
  - Password: `Coord@123`
  - Role: Coordinator – reviews **Submitted** claims and forwards them to the Academic Manager or rejects them.

- **Academic Manager**
  - Email: `manager@cmcs.test`
  - Password: `Man@123`
  - Role: Manager – reviews **Pending Review** claims and gives the final approval or rejection.

- **HR (Super User)**
  - Email: `hr@cmcs.test`
  - Password: `Hr@123`
  - Role: HR – manages lecturer profiles (rates, active flags) and views invoices and monthly summaries.

> In this prototype, credentials are used to document the roles consistently for the marker. The application itself uses a role selector and session state instead of full Identity login as allowed by the POE.


## 6. Database and SSMS Script

The application uses SQL Server with Entity Framework Core.

Main tables:

- **Lecturers**
  - `Id`
  - `Name`
  - `Email`
  - `Phone`
  - `Department`
  - `HourlyRate`
  - `IsActive`

- **Claims**
  - `Id`
  - `LecturerId` (FK)
  - `LecturerName` (denormalised for convenience)
  - `Month` (string, e.g. “October 2025”)
  - `HoursWorked`
  - `HourlyRate`
  - `Notes` (optional)
  - `AttachmentFileName`
  - `AttachmentStoredName`
  - `Status` (`ClaimStatus` enum)
  - `CalculatedAmount` (computed property: `HoursWorked * HourlyRate`)

A SQL script of the final database (tables and relationships) is generated from SSMS and included with the submission as required by the POE.

## 7. How to Run the Project

1. **Prerequisites**
   - .NET SDK (matching the project’s target framework, e.g. .NET 6 or later).
   - SQL Server + SQL Server Management Studio (SSMS).

2. **Database Setup**
   - Open SSMS.
   - Create a database (e.g CMCS) or use the name expected in your `appsettings.json`.
   - Run the provided SQL script to create tables and seed any required starter data (if included).
   - Confirm that the connection string points to the correct database.

3. **Configure Connection String**
   - In `appsettings.json`, update:

     ```json
     "ConnectionStrings": {
       "CMCSConnection": "Server=YOUR_SERVER;Database=CMCS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
     }
     ```

4. **Run the Application**
   - Open the solution in Visual Studio.
   - Set the web project as the startup project.
   - Press F5 to run or use:

     ```bash
     dotnet run
     ```

5. **Using the System**
   - Browse to the home page.
   - Use the **"Choose role"** / **Select Role** screen to simulate each role.
   - Navigate through:
     - Lecturer → Submit Claim
     - Coordinator → Review
     - Manager → Manager Review
     - HR → Dashboard & Lecturers


## 8. Validation, Automation, and Reporting

### Validation

- Server side checks in ClaimsController ensure:
  - Hours > 0 and ≤ a maximum monthly limit.
  - Hourly Rate within a configured range.
  - No duplicate claim for the same lecturer and month unless the previous one was rejected.
  - File extension and size constraints.

- Client side validation:
  - Standard MVC validation messages.
  - Extra soft validation and confirmation prompts for empty/zero fields.
  - File type and size validation before upload.

### Automation

- Total claim amount is automatically calculated on the client (Hours × Rate) and displayed as the lecturer types.
- Rate is auto-filled and read-only, based on the lecturer selected from the HR-managed list.
- Simple auto-check in approval flow (AutoValidateClaim) flags suspicious or out-of-range values.

### Reporting

- HR Monthly Summary:
  - Total hours and amounts per lecturer for a selected month.
  - Grand totals for hours and values.
- Per-Lecturer Invoice:
  - Print-ready HTML, intended to be exported as PDF.
- CSV exports for approved claims.

## 9. Version Control

The project is managed via Git with a minimum of 10 commits that reflect:

- Initial project setup.
- Database and models.
- Lecturer submission and validation.
- Coordinator and Manager workflow.
- HR dashboard and invoice.
- Styling and UI refinements.
- Final clean-up and documentation.

