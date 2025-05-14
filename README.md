# 🏥 Hospital Appointment System MVC

This is a Hospital Appointment Management System built using ASP.NET MVC. The system is designed to manage appointments between **Admins**, **Doctors**, and **Patients**. It provides role-based access and functionalities tailored to each user type.

---

## 👥 User Roles

### 🔸 Admin
The Admin has full control over the system, including the ability to:
- **Dashboard**: 
  - View key statistics including:
    - Total doctors
    - Total patients
    - Total appointments
    - Today's appointments
    - Cancelled appointments
    - Total feedbacks
  - Interactive charts for visual insights.
- **Manage Doctors**:
  - View doctors list and add new doctor with availability. 
  - Add, edit, delete, and set availability of doctor.
  - View particular doctor's appointments and schedules.
  - On adding a new doctor, the system sends a temporary username and password to the doctor via email.
  - On first login, doctors are required to update their username and password.
- **Manage Patients**:
  - View and edit patient details.
  - View particular patient's appointments.
- **View Appointments**:
  - Monitor all appointments (today’s, cancelled, and total).
  - Search appointments based on any field.
- **Unavailability Management**:
  - Can mark hospital or doctor as unavailable for specific dates.
  - The system automatically notifies all affected doctors and patients via email.

---

### 🩺 Doctor
Doctors have personalized access to:
- **Appointment Schedule**
  - View all booked appointments.
  - Each time slot can have up to **4 patients maximum**.
  - Slots exceeding 4 bookings will be hidden from patients.
- **Appointment Actions**
  - Mark appointments as **Completed**, **Cancelled**, or **Missed**.
  - Add and edit prescriptions for completed appointments.
- **Availability**
  - View own schedules & report unavailability for future.
  - Receive alerts for hospital unavailability via email.
- **Account Management**
  - Update username and password.
  - Use **Forgot Password** functionality with **OTP verification via email**.


---

### 👤 Patient
Patients can:
- **Book Appointments**
  - Book with available doctors.
  - System prevents:
    - Booking on doctor’s unavailable dates
    - Hospital unavailable days
    - Already fully booked time slots (4 max per slot per doctor)
    - Overlapping appointments
- **Cancel Appointments**
  - Provide a reason when cancelling.
- **View Appointment History**
  - See status: Booked, Cancelled, Missed, Completed
  - View prescriptions (if any)
- **Account Management**
  - Update username and password.
  - Use **Forgot Password** functionality with **OTP verification via email**.

---

## 🧠 Booking Logic & Constraints
- Prevents double bookings or time slot clashes.
- Checks for:
  - Doctor unavailability
  - Hospital closure
  - Existing appointments for the same time
  - Time slot capacity (max 4 patients per slot)
- Displays only valid and bookable slots to patients.

---

## 📊 Features Overview
- 🔐 Role-based secure login
- 📈 Admin Dashboard with charts
- 📅 Smart Appointment Scheduling
- 📬 Email Notifications (Credential sharing, cancellations, unavailability alerts, OTP)
- 🧾 Prescription Management
- 🔁 Password reset and account updates

---

## 📌 Technologies Used
- ASP.NET MVC Framework
- SQL Server (Database)
- HTML/CSS/JavaScript
- Bootstrap (for UI)
- SMTP (for sending email notifications)

---

## 📬 Contact
For any issues or contributions, feel free to raise a GitHub issue or contact the maintainer.

---

