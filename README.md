# Envelope Steward

A church envelope stewardship desktop application for tracking member donations, generating reports, and producing CRA-compliant tax receipts.

Built for **Osgoode Baptist and Vernon United Churches**.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| UI | .NET 10 · Windows Forms |
| Database | SQLite (`Data/envelopes.db`) |
| PDF Receipts | QuestPDF 2024.x (Community License) |
| Language | C# 13 |

---

## Quick Start

1. **Build & Run** — open `Envelope Steward.slnx` in Visual Studio 2022+ and press F5, or run `dotnet run` from the project folder.
2. **First run** — the SQLite database is created automatically at `bin/Debug/net10.0-windows/Data/envelopes.db`.
3. **Import seed data** — use **File → Import Members CSV** and **File → Import Offering Types CSV** and point to the files in `SeedData/`.
4. **Fill in Settings** — go to the **Settings** tab and enter your church name, address, CRA registration number, and authorized signer before generating any receipts.

---

## Features

### Members (Tab 1)
- Full CRUD: Add, Edit, Delete members
- Search by envelope number, name, or city
- Fields: Envelope #, First Name, Last Name, Address, Phone, Email, Full Member, Shut-In, Active flags
- **CSV import is dynamic** — any column in your CSV that isn't a known field is automatically added to the database

### Offering Types (Tab 2)
- Full CRUD for offering/fund categories
- **Tax Receiptable** flag — uncheck for items like fundraising revenue or HST rebates that don't qualify for CRA tax receipts
- CSV import is also dynamic

### Donations (Tab 3)
- Entry form: pick member (by envelope # or name), date, offering type, amount, notes
- History grid with filter by year and/or member
- Edit or delete any past entry

### Reports (Tab 4)
- **By Member** — total donations per donor for the period
- **By Offering Type** — total per fund
- **Tax Receipt Summary** — all members with qualifying donation totals (ready for receipts)
- Period: Annual, Quarterly, Monthly
- Export any report to CSV
- **Generate Tax Receipts (PDF)** — one PDF per member, 3 receipt copies per page (see below)

### Settings (Tab 5)
- Church name, street address, city, province, postal code
- CRA charitable registration number (e.g. `119070605 RR0001`)
- Authorized signer name
- Church logo (PNG/JPG) — appears on receipts
- Next receipt number — auto-increments on each batch

---

## Tax Receipt PDF Format

Receipts match the CRA official receipt format used by the church. Each PDF contains **3 identical copies** of the receipt on one letter-size page (one for church records, one for the donor, one spare).

Each receipt shows:
- Church name, address, and CRA registration number
- "OFFICIAL RECEIPT" header with the tax year
- Total qualifying donations for the year
- Donor name, address, and envelope number
- Receipt number (sequential), date issued
- Authorized signature line

PDFs are saved to:  
`My Documents\EnvelopeSteward\Receipts_YYYY\Receipt_NNNN_MemberName_YYYY.pdf`

---

## CSV Import — Dynamic Column Handling

When you import a CSV file, Envelope Steward:

1. Reads all column headers from the CSV
2. Maps known headers to the correct database fields (see tables below)
3. **Any column not in the known list is sanitized and added to the database automatically** — no schema changes needed
4. Upserts rows (updates existing records, inserts new ones) keyed on Envelope # for members

### Members CSV — Known Column Aliases

| CSV Header (any casing) | Database Column |
|------------------------|----------------|
| Envelope #, Envelope Number | `EnvelopeNumber` |
| Name, Full Name | split into First + Last |
| First, First Name | `FirstName` |
| Last, Last Name, Surname | `LastName` |
| Street Address, Address | `StreetAddress` |
| City | `City` |
| Province, State | `Province` |
| Postal Code, Postal, Zip | `PostalCode` |
| Home Phone, Phone | `HomePhone` |
| Email, E-Mail | `Email` |
| Full Member | `FullMember` |
| Shut In, Shut-In | `ShutIn` |
| Active | `Active` |
| Status | `Status` |

### Offering Types CSV — Known Column Aliases

| CSV Header (any casing) | Database Column |
|------------------------|----------------|
| Offering Id, ID, Code | `Name` (the short code) |
| Description, Name | `Description` |
| Tax Receiptable | `TaxReceiptable` |

---

## File Structure

```
Envelope Steward/
├── README.md                        ← you are here
├── Envelope Steward.slnx            ← solution file
├── Envelope Steward.csproj          ← project (net10.0-windows)
│
├── Program.cs                       ← entry point, QuestPDF license
├── Form1.cs / Form1.Designer.cs     ← main form (5-tab layout)
│
├── Models/
│   ├── MemberRecord.cs
│   ├── OfferingTypeRecord.cs
│   ├── DonationRecord.cs
│   └── ChurchSettings.cs
│
├── Data/
│   ├── DataAccess.cs                ← all SQLite operations
│   └── envelopes.db                 ← created at runtime (not in source control)
│
├── Forms/
│   ├── MemberEditForm.cs            ← Add/Edit member dialog
│   ├── OfferingTypeEditForm.cs      ← Add/Edit offering type dialog
│   └── DonationEditForm.cs          ← Edit donation dialog
│
├── Services/
│   └── PdfReceiptService.cs         ← QuestPDF receipt generation
│
├── SeedData/
│   ├── sample_members.csv           ← template with common member fields
│   └── sample_offering_types.csv    ← template with common offering categories
│
└── Docs/
    └── architecture.svg             ← app architecture diagram
```

---

## Database Tables

| Table | Purpose |
|-------|---------|
| `Members` | Donor roster, keyed by envelope number |
| `OfferingTypes` | Fund/category list with tax-receiptable flag |
| `Donations` | Individual giving records (member + type + amount + date) |
| `ChurchSettings` | Key/value store for church info and settings |
| `Receipts` | Audit log of issued tax receipts |

---

## Notes

- The SQLite vulnerability warning (`SQLitePCLRaw.lib.e_sqlite3 2.1.2`) comes from the transitive dependency of `Microsoft.Data.Sqlite 7.0.0`. To resolve, upgrade to `Microsoft.Data.Sqlite 8.0.0` or later when ready.
- QuestPDF Community License is free for organizations with revenue under $1M USD/year. No watermarks, no limitations.
- The `envelopes.db` file should be backed up regularly — it is the sole data store.
