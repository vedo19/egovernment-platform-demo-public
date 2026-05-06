# E-Government Platform — User Guide

Welcome to the E-Government Platform! This guide explains how to use the application as a regular user — no technical knowledge needed.

---

## What Is This Platform?

The E-Government Platform is an online system where citizens can:

- Create a personal profile with their information
- Apply for permits (building permits, business permits, etc.)
- File complaints about public services
- Request official documents (birth certificates, national ID, marriage certificate, etc.)
- Track the status of all their requests in one dashboard

Government employees (Admins and Officers) use the same platform to review, process, and manage all citizen requests.

---

## Three User Roles

The platform has three types of users, each with different access levels:

### 1. Citizen
A regular user who needs government services.

**What you can do:**
- Register an account and create your personal profile
- Submit service requests (permits or complaints)
- Request official documents
- View the status of all your submissions
- Update your profile information

### 2. Officer
A government employee assigned to process specific requests.

**What you can do:**
- View service requests and documents assigned to you
- Review the full details of each assigned task
- Approve or reject requests with mandatory reasons for rejections
- Citizens will see your notes and rejection reasons on their dashboard

### 3. Admin
A system administrator with full control.

**What you can do:**
- View ALL service requests and documents from all citizens
- Assign Officers to specific requests
- Change any user's role (promote a Citizen to Officer, or demote back)
- Update the status of any request or document
- Full management of all users

---

## Getting Started

### Step 1: Open the Platform

Open your browser and go to:

```
http://localhost:3000
```

You'll see the Sign In page.

### Step 2: Create an Account

1. Click **"Register"** (link at the bottom of the Sign In page)
2. Fill in:
   - **Full Name** — your real name
   - **Email** — your email address (this will be your login)
   - **Password** — minimum 6 characters
3. Click **"Create Account"**

All new accounts are created as **Citizens**. Only an Admin can promote a user to Officer or Admin.

**Pre-configured Admin Account:**
- Email: set locally via `ADMIN_EMAIL` in your `.env`
- Password: set locally via `ADMIN_PASSWORD` in your `.env`

Use this account to log in as Admin and manage users.

### Step 3: Log In (if you already have an account)

1. Enter your **email** and **password**
2. Click **"Sign In"**

---

## Citizen Dashboard

After logging in as a Citizen, you'll see your dashboard with three tabs:

### Profile Tab

This is where you set up your personal information.

**First time? Create your profile:**
1. Click **"Create Profile"**
2. Fill in all fields:
   - **Phone Number** — your contact number
   - **Address** — your street address
   - **Date of Birth** — select from the calendar
   - **National ID** — your national ID number
   - **City** — your city of residence
   - **Gender** — select Male or Female
3. Click **"Save"**

**Update your profile later:**
1. Click **"Edit Profile"**
2. Change any field
3. Click **"Save"**

### Service Requests Tab

This is where you apply for permits or file complaints.

**Submit a new request:**
1. Click **"New Request"**
2. Select the **Type**:
   - **Permit** — for building permits, business permits, etc.
   - **Complaint** — for issues with public services
3. Enter a **Title** — brief summary (e.g., "Building Permit for Home Extension")
4. Enter a **Description** — detailed explanation of what you need
5. Click **"Submit"**

**Track your requests:**
Your requests appear in a table showing:
- **Type** — Permit or Complaint
- **Title** — your request title
- **Status** — current workflow state:
   - Permit: Submitted, OfficerAssigned, AwaitingDocuments, UnderReview, DocumentsRejected, Approved, Rejected
   - Complaint: Submitted, OfficerAssigned, UnderReview, Approved, Rejected
- **Progress** — visual percentage bar from submission to final decision
- **Officer Note** — appears when documents are requested or rejected
- **Upload PDF** — appears only when status is AwaitingDocuments or DocumentsRejected
- **Uploaded PDF** — lets you open previously uploaded file
- **Created** — when you submitted it

**Permit document cycle:**
1. Officer requests document with note.
2. Citizen uploads PDF.
3. Officer can view/download PDF and either:
    - approve request, or
    - reject documents (citizen can re-upload), or
    - reject request as final.

### Documents Tab

This is where you request official documents.

**Request a new document:**
1. Click **"Request Document"**
2. Select the **Document Type**:
   - Birth Certificate
   - National ID
   - Marriage Certificate
   - Death Certificate
   - Driving License
3. Enter a **Title** — e.g., "Replacement Birth Certificate"
4. Optionally add a **Description** with any extra details
5. Click **"Submit"**

**Track your documents:**
Your document requests appear in a table showing:
- **Type** — which document you requested
- **Status**:
   - Submitted
   - UnderReview
   - Approved
  - 🔴 **Rejected** — request was denied
- **Progress** — visual workflow percentage bar
- **Reason** — if rejected, the officer's reason will appear here
- **Reference #** — when your document is ready, you'll get a reference number (e.g., "MC-20260307-ABC12345")
- **Created** — when you submitted the request

---

## Admin Dashboard

After logging in as an Admin, you see a management dashboard with tabs:

### Service Requests Tab

Displays ALL service requests from ALL citizens.

**Filter requests:**
- Use the dropdown at the top right to filter by status (Pending, In Progress, Resolved, Rejected)

**Change request status:**
- Use the status dropdown in the "Actions" column to change a request's status
- For example: move a Pending request to "In Progress" when you start working on it

**Assign an Officer:**
- Use the "Assign Officer" dropdown to assign a specific officer to handle the request
- Once assigned, the request moves to "In Progress" and appears on the Officer's dashboard

### Documents Tab

Displays ALL document requests from ALL citizens.

**Filter documents:**
- Use the dropdown to filter by status

**Change document status:**
- Use the status dropdown in "Actions" to update
- When you mark a document as "Ready", a reference number is automatically generated

**Assign an Officer:**
- Use the "Assign Officer" dropdown to assign an officer

### Users Tab

Displays all registered users. This is where you manage roles.

**Change a user's role:**
1. Find the user in the table
2. Use the **"Change Role"** dropdown next to their name
3. Select the new role: **Citizen**, **Officer**, or **Admin**
4. The change takes effect immediately

This is how you promote citizens to officers — no invitation codes needed.

---

## Officer Dashboard

After logging in as an Officer, you see your personal task dashboard with two tabs:

### My Requests Tab

Shows only the service requests that have been assigned to you by an Admin.

**Review a request:**
1. Click the **"Review"** button on any request
2. You'll see the full details: type, title, description, citizen ID, creation date
3. Read through the citizen's description carefully
4. If a supporting PDF was uploaded, use **View Submitted PDF** or **Download PDF**

**Approve a request:**
1. Optionally type notes in the text box (e.g., “Approved, permit valid for 12 months”)
2. Click **"Approve"** — the request status changes to "Approved"

**Reject a request:**
1. You **must** type a rejection reason in the text box (e.g., "Missing construction blueprints required by regulation 3.2")
2. Click **"Reject"** — the citizen will see this reason on their dashboard

**Request document for permit:**
1. In OfficerAssigned permit cases, click **Request Documents**.
2. Provide a clear note describing required PDF.

**Reject only documents (resubmittable):**
1. In permit UnderReview with uploaded file, click **Reject Documents**.
2. Citizen status becomes DocumentsRejected and can upload again.

### My Documents Tab

Shows only the document requests assigned to you.

**Approve a document:**
1. Click "Review" to see full details
2. Click **"Approve"** — the document status changes to "Ready" and a reference number is generated

**Reject a document:**
1. Type a rejection reason (mandatory)
2. Click **"Reject"** — the citizen will see the reason on their dashboard

---

## Logging Out

Click the **"Logout"** button in the top-right corner of the navigation bar. You'll be taken back to the Sign In page.

---

## Tips

- **Bookmark the page**: Save `http://localhost:3000` for easy access
- **Stay informed**: Check your dashboard regularly to see status updates on your requests
- **Be detailed**: When submitting requests, provide as much detail as possible — this helps officers process your request faster
- **Keep your profile updated**: Make sure your phone number and address are current so officers can reach you if needed

---

## Frequently Asked Questions

**Q: I forgot my password. How do I reset it?**
A: Password reset is not yet available. Contact your system administrator to create a new account.

**Q: Can I cancel a request after submitting it?**
A: You cannot cancel a request through the platform. Contact the relevant office directly.

**Q: How long does it take to process my request?**
A: Processing times depend on the type of request and current workload. Check your dashboard for status updates.

**Q: Why was my request rejected?**
A: An officer reviewed your request and provided a reason. Check the **"Notes"** column for service requests or the **"Reason"** column for documents on your dashboard to see the officer's explanation. You may submit a new request with corrected information.

**Q: How can we test this quickly with Postman?**
A: Import `docs/postman/Workflow-Demo.postman_collection.json`, set collection variables (baseUrl, user credentials, tokens, ids), then run requests in sequence: login users, create permit, assign officer, request documents, upload PDF, reject documents, reupload, approve.

**Q: I see a blank screen. What do I do?**
A: Try clearing your browser's cache (Ctrl+Shift+Delete), or open the page in a private/incognito window. If that doesn't work, make sure the server is running.
