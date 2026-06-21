using System.Data;
using Envelope_Steward.Models;
using Microsoft.Data.Sqlite;

namespace Envelope_Steward
{
    public static class DataAccess
    {
        private static string DataFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        public  static string DbPath     => Path.Combine(DataFolder, "envelopes.db");
        private static string ConnectionString => $"Data Source={DbPath}";

        public static void DeleteDatabase()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(DbPath)) File.Delete(DbPath);
        }

        // ── Schema ──────────────────────────────────────────────────────────

        public static void EnsureDatabase()
        {
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Members (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EnvelopeNumber TEXT,
    FirstName TEXT,
    LastName TEXT,
    Status TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Members_Envelope ON Members(EnvelopeNumber)
    WHERE EnvelopeNumber IS NOT NULL AND EnvelopeNumber != '';

CREATE TABLE IF NOT EXISTS OfferingTypes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT UNIQUE,
    Description TEXT
);

CREATE TABLE IF NOT EXISTS Donations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MemberId INTEGER,
    OfferingTypeId INTEGER,
    Amount REAL,
    Date TEXT,
    FOREIGN KEY(MemberId) REFERENCES Members(Id),
    FOREIGN KEY(OfferingTypeId) REFERENCES OfferingTypes(Id)
);

CREATE TABLE IF NOT EXISTS ChurchSettings (
    Key TEXT PRIMARY KEY,
    Value TEXT
);

CREATE TABLE IF NOT EXISTS Receipts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReceiptNumber INTEGER,
    MemberId INTEGER,
    TaxYear INTEGER,
    TotalAmount REAL,
    DateIssued TEXT,
    PdfPath TEXT
);
";
            cmd.ExecuteNonQuery();

            EnsureColumns(conn, "Members", new Dictionary<string, string>
            {
                ["StreetAddress"] = "TEXT",
                ["City"] = "TEXT",
                ["Province"] = "TEXT",
                ["PostalCode"] = "TEXT",
                ["HomePhone"] = "TEXT",
                ["Email"] = "TEXT",
                ["FullMember"] = "TEXT",
                ["ShutIn"] = "TEXT",
                ["Active"] = "TEXT"
            });

            EnsureColumns(conn, "OfferingTypes", new Dictionary<string, string>
            {
                ["TaxReceiptable"] = "INTEGER NOT NULL DEFAULT 1"
            });

            EnsureColumns(conn, "Donations", new Dictionary<string, string>
            {
                ["Notes"] = "TEXT"
            });
        }

        private static void EnsureColumns(SqliteConnection conn, string table, Dictionary<string, string> desired)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var p = conn.CreateCommand())
            {
                p.CommandText = $"PRAGMA table_info('{table}')";
                using var r = p.ExecuteReader();
                while (r.Read()) existing.Add(r.GetString(1));
            }
            foreach (var kv in desired)
            {
                if (!existing.Contains(kv.Key))
                {
                    using var a = conn.CreateCommand();
                    a.CommandText = $"ALTER TABLE {table} ADD COLUMN {kv.Key} {kv.Value};";
                    a.ExecuteNonQuery();
                }
            }
        }

        // ── Members ─────────────────────────────────────────────────────────

        public static List<MemberRecord> GetAllMembers()
        {
            EnsureDatabase();
            var list = new List<MemberRecord>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, EnvelopeNumber, FirstName, LastName,
                StreetAddress, City, Province, PostalCode, HomePhone, Email,
                FullMember, ShutIn, Active, Status
                FROM Members ORDER BY EnvelopeNumber";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadMember(r));
            return list;
        }

        public static MemberRecord? GetMember(int id)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, EnvelopeNumber, FirstName, LastName,
                StreetAddress, City, Province, PostalCode, HomePhone, Email,
                FullMember, ShutIn, Active, Status
                FROM Members WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadMember(r) : null;
        }

        public static int AddMember(MemberRecord m)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Members
                (EnvelopeNumber,FirstName,LastName,StreetAddress,City,Province,
                 PostalCode,HomePhone,Email,FullMember,ShutIn,Active,Status)
                VALUES($env,$first,$last,$street,$city,$prov,$postal,$phone,$email,
                       $fm,$shut,$active,$status);
                SELECT last_insert_rowid();";
            SetMemberParams(cmd, m);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void UpdateMember(MemberRecord m)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Members SET
                EnvelopeNumber=$env, FirstName=$first, LastName=$last,
                StreetAddress=$street, City=$city, Province=$prov,
                PostalCode=$postal, HomePhone=$phone, Email=$email,
                FullMember=$fm, ShutIn=$shut, Active=$active, Status=$status
                WHERE Id=$id";
            SetMemberParams(cmd, m);
            cmd.Parameters.AddWithValue("$id", m.Id);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteMember(int id)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Members WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public static DataTable GetMembersDataTable(string search = "")
        {
            EnsureDatabase();
            var dt = new DataTable();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            if (string.IsNullOrWhiteSpace(search))
            {
                cmd.CommandText = @"SELECT Id,
                    CAST(EnvelopeNumber AS TEXT) as 'Envelope #',
                    CAST(FirstName AS TEXT) as 'First Name',
                    CAST(LastName AS TEXT) as 'Last Name',
                    CAST(City AS TEXT) as 'City',
                    CAST(Province AS TEXT) as 'Province',
                    CAST(HomePhone AS TEXT) as 'Phone',
                    CAST(Email AS TEXT) as 'Email',
                    CAST(Active AS TEXT) as 'Active'
                    FROM Members ORDER BY EnvelopeNumber";
            }
            else
            {
                cmd.CommandText = @"SELECT Id,
                    CAST(EnvelopeNumber AS TEXT) as 'Envelope #',
                    CAST(FirstName AS TEXT) as 'First Name',
                    CAST(LastName AS TEXT) as 'Last Name',
                    CAST(City AS TEXT) as 'City',
                    CAST(Province AS TEXT) as 'Province',
                    CAST(HomePhone AS TEXT) as 'Phone',
                    CAST(Email AS TEXT) as 'Email',
                    CAST(Active AS TEXT) as 'Active'
                    FROM Members
                    WHERE EnvelopeNumber LIKE $s
                       OR FirstName LIKE $s
                       OR LastName LIKE $s
                       OR City LIKE $s
                    ORDER BY EnvelopeNumber";
                cmd.Parameters.AddWithValue("$s", $"%{search}%");
            }
            using var r = cmd.ExecuteReader();
            dt.Load(r);
            return SanitizeForGrid(dt);
        }

        private static MemberRecord ReadMember(SqliteDataReader r) => new()
        {
            Id = r.GetInt32(0),
            EnvelopeNumber = r.IsDBNull(1) ? "" : r.GetString(1),
            FirstName = r.IsDBNull(2) ? "" : r.GetString(2),
            LastName = r.IsDBNull(3) ? "" : r.GetString(3),
            StreetAddress = r.IsDBNull(4) ? "" : r.GetString(4),
            City = r.IsDBNull(5) ? "" : r.GetString(5),
            Province = r.IsDBNull(6) ? "" : r.GetString(6),
            PostalCode = r.IsDBNull(7) ? "" : r.GetString(7),
            HomePhone = r.IsDBNull(8) ? "" : r.GetString(8),
            Email = r.IsDBNull(9) ? "" : r.GetString(9),
            FullMember = !r.IsDBNull(10) && r.GetString(10).Equals("Y", StringComparison.OrdinalIgnoreCase),
            ShutIn = !r.IsDBNull(11) && r.GetString(11).Equals("Y", StringComparison.OrdinalIgnoreCase),
            Active = !r.IsDBNull(12) && r.GetString(12).Equals("Y", StringComparison.OrdinalIgnoreCase),
            Status = r.IsDBNull(13) ? "" : r.GetString(13)
        };

        private static void SetMemberParams(SqliteCommand cmd, MemberRecord m)
        {
            cmd.Parameters.AddWithValue("$env", (object?)m.EnvelopeNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$first", m.FirstName);
            cmd.Parameters.AddWithValue("$last", m.LastName);
            cmd.Parameters.AddWithValue("$street", m.StreetAddress);
            cmd.Parameters.AddWithValue("$city", m.City);
            cmd.Parameters.AddWithValue("$prov", m.Province);
            cmd.Parameters.AddWithValue("$postal", m.PostalCode);
            cmd.Parameters.AddWithValue("$phone", m.HomePhone);
            cmd.Parameters.AddWithValue("$email", m.Email);
            cmd.Parameters.AddWithValue("$fm", m.FullMember ? "Y" : "N");
            cmd.Parameters.AddWithValue("$shut", m.ShutIn ? "Y" : "N");
            cmd.Parameters.AddWithValue("$active", m.Active ? "Y" : "N");
            cmd.Parameters.AddWithValue("$status", m.Status);
        }

        // ── Offering Types ───────────────────────────────────────────────────

        public static List<OfferingTypeRecord> GetAllOfferingTypes()
        {
            EnsureDatabase();
            var list = new List<OfferingTypeRecord>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Description, TaxReceiptable FROM OfferingTypes ORDER BY Description, Name";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new OfferingTypeRecord
                {
                    Id = r.GetInt32(0),
                    Name = r.IsDBNull(1) ? "" : r.GetString(1),
                    Description = r.IsDBNull(2) ? "" : r.GetString(2),
                    TaxReceiptable = r.IsDBNull(3) || r.GetInt32(3) != 0
                });
            return list;
        }

        public static OfferingTypeRecord? GetOfferingType(int id)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Description, TaxReceiptable FROM OfferingTypes WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new OfferingTypeRecord
            {
                Id = r.GetInt32(0),
                Name = r.IsDBNull(1) ? "" : r.GetString(1),
                Description = r.IsDBNull(2) ? "" : r.GetString(2),
                TaxReceiptable = r.IsDBNull(3) || r.GetInt32(3) != 0
            };
        }

        public static int AddOfferingType(OfferingTypeRecord t)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO OfferingTypes (Name, Description, TaxReceiptable)
                VALUES ($name, $desc, $tr);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$name", t.Name);
            cmd.Parameters.AddWithValue("$desc", t.Description);
            cmd.Parameters.AddWithValue("$tr", t.TaxReceiptable ? 1 : 0);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void UpdateOfferingType(OfferingTypeRecord t)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE OfferingTypes SET Name=$name, Description=$desc, TaxReceiptable=$tr WHERE Id=$id";
            cmd.Parameters.AddWithValue("$name", t.Name);
            cmd.Parameters.AddWithValue("$desc", t.Description);
            cmd.Parameters.AddWithValue("$tr", t.TaxReceiptable ? 1 : 0);
            cmd.Parameters.AddWithValue("$id", t.Id);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteOfferingType(int id)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM OfferingTypes WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public static DataTable GetOfferingTypesDataTable()
        {
            EnsureDatabase();
            var dt = new DataTable();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id,
                CASE WHEN Description IS NOT NULL AND CAST(Description AS TEXT) != '' THEN CAST(Description AS TEXT) ELSE CAST(Name AS TEXT) END as 'Name / Description',
                CAST(Name AS TEXT) as 'Code',
                CASE WHEN TaxReceiptable = 1 THEN 'Yes' ELSE 'No' END as 'Tax Receiptable'
                FROM OfferingTypes ORDER BY Description, Name";
            using var r = cmd.ExecuteReader();
            dt.Load(r);
            return SanitizeForGrid(dt);
        }

        // ── Donations ────────────────────────────────────────────────────────

        public static int AddDonation(DonationRecord d)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Donations (MemberId, OfferingTypeId, Amount, Date, Notes)
                VALUES ($mid, $oid, $amt, $date, $notes);
                SELECT last_insert_rowid();";
            SetDonationParams(cmd, d);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void UpdateDonation(DonationRecord d)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Donations SET MemberId=$mid, OfferingTypeId=$oid,
                Amount=$amt, Date=$date, Notes=$notes WHERE Id=$id";
            SetDonationParams(cmd, d);
            cmd.Parameters.AddWithValue("$id", d.Id);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteDonation(int id)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Donations WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public static DonationRecord? GetDonation(int id)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT d.Id, d.MemberId, m.FirstName||' '||m.LastName, m.EnvelopeNumber,
                d.OfferingTypeId,
                CASE WHEN ot.Description != '' THEN ot.Description ELSE ot.Name END,
                d.Amount, d.Date, d.Notes
                FROM Donations d
                LEFT JOIN Members m ON d.MemberId=m.Id
                LEFT JOIN OfferingTypes ot ON d.OfferingTypeId=ot.Id
                WHERE d.Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadDonation(r) : null;
        }

        public static DataTable GetDonationsDataTable(int? memberId = null, int? year = null, int? offeringTypeId = null)
        {
            EnsureDatabase();
            var dt = new DataTable();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();

            var where = new List<string>();
            if (memberId.HasValue) { where.Add("d.MemberId=$mid"); cmd.Parameters.AddWithValue("$mid", memberId.Value); }
            if (year.HasValue) { where.Add("strftime('%Y',d.Date)=$yr"); cmd.Parameters.AddWithValue("$yr", year.Value.ToString()); }
            if (offeringTypeId.HasValue) { where.Add("d.OfferingTypeId=$oid"); cmd.Parameters.AddWithValue("$oid", offeringTypeId.Value); }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

            cmd.CommandText = $@"SELECT d.Id,
                CAST(d.Date AS TEXT) as 'Date',
                CAST(m.EnvelopeNumber AS TEXT) as 'Env #',
                CAST(m.FirstName AS TEXT)||' '||CAST(m.LastName AS TEXT) as 'Member',
                CASE WHEN ot.Description IS NOT NULL AND CAST(ot.Description AS TEXT) != '' THEN CAST(ot.Description AS TEXT) ELSE CAST(ot.Name AS TEXT) END as 'Offering Type',
                CAST(d.Amount AS REAL) as 'Amount',
                CAST(d.Notes AS TEXT) as 'Notes'
                FROM Donations d
                LEFT JOIN Members m ON d.MemberId=m.Id
                LEFT JOIN OfferingTypes ot ON d.OfferingTypeId=ot.Id
                {whereClause}
                ORDER BY d.Date DESC, m.EnvelopeNumber";
            using var r = cmd.ExecuteReader();
            dt.Load(r);
            return SanitizeForGrid(dt);
        }

        public static decimal GetMemberYearTotal(int memberId, int year)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT COALESCE(SUM(d.Amount),0) FROM Donations d
                JOIN OfferingTypes ot ON d.OfferingTypeId=ot.Id
                WHERE d.MemberId=$mid AND strftime('%Y',d.Date)=$yr
                AND (ot.TaxReceiptable=1 OR ot.TaxReceiptable IS NULL)";
            cmd.Parameters.AddWithValue("$mid", memberId);
            cmd.Parameters.AddWithValue("$yr", year.ToString());
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
        }

        public static List<int> GetAvailableYears()
        {
            EnsureDatabase();
            var list = new List<int>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT CAST(strftime('%Y',Date) AS INTEGER) as Y FROM Donations WHERE Date IS NOT NULL ORDER BY Y DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetInt32(0));
            if (list.Count == 0) list.Add(DateTime.Today.Year);
            return list;
        }

        private static DonationRecord ReadDonation(SqliteDataReader r) => new()
        {
            Id = r.GetInt32(0),
            MemberId = r.IsDBNull(1) ? 0 : r.GetInt32(1),
            MemberName = r.IsDBNull(2) ? "" : r.GetString(2),
            EnvelopeNumber = r.IsDBNull(3) ? "" : r.GetString(3),
            OfferingTypeId = r.IsDBNull(4) ? 0 : r.GetInt32(4),
            OfferingTypeName = r.IsDBNull(5) ? "" : r.GetString(5),
            Amount = r.IsDBNull(6) ? 0m : Convert.ToDecimal(r.GetDouble(6)),
            Date = r.IsDBNull(7) ? DateTime.Today : DateTime.Parse(r.GetString(7)),
            Notes = r.IsDBNull(8) ? "" : r.GetString(8)
        };

        private static void SetDonationParams(SqliteCommand cmd, DonationRecord d)
        {
            cmd.Parameters.AddWithValue("$mid", d.MemberId);
            cmd.Parameters.AddWithValue("$oid", d.OfferingTypeId);
            cmd.Parameters.AddWithValue("$amt", (double)d.Amount);
            cmd.Parameters.AddWithValue("$date", d.Date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$notes", d.Notes ?? "");
        }

        // ── Reports ──────────────────────────────────────────────────────────

        public static DataTable GetReportByMember(int year, string period)
        {
            EnsureDatabase();
            var dt = new DataTable();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.Parameters.AddWithValue("$yr", year.ToString());

            string periodCol = period switch
            {
                "Monthly" => "strftime('%m',d.Date) as 'Month',",
                "Quarterly" => "CASE CAST(strftime('%m',d.Date) AS INTEGER) WHEN 1 THEN 'Q1' WHEN 2 THEN 'Q1' WHEN 3 THEN 'Q1' WHEN 4 THEN 'Q2' WHEN 5 THEN 'Q2' WHEN 6 THEN 'Q2' WHEN 7 THEN 'Q3' WHEN 8 THEN 'Q3' WHEN 9 THEN 'Q3' ELSE 'Q4' END as 'Quarter',",
                _ => ""
            };

            string groupBy = period switch
            {
                "Monthly" => ", strftime('%m',d.Date)",
                "Quarterly" => ", CASE CAST(strftime('%m',d.Date) AS INTEGER) WHEN 1 THEN 1 WHEN 2 THEN 1 WHEN 3 THEN 1 WHEN 4 THEN 2 WHEN 5 THEN 2 WHEN 6 THEN 2 WHEN 7 THEN 3 WHEN 8 THEN 3 WHEN 9 THEN 3 ELSE 4 END",
                _ => ""
            };

            cmd.CommandText = $@"SELECT
                m.EnvelopeNumber as 'Env #',
                m.FirstName||' '||m.LastName as 'Member',
                {periodCol}
                SUM(d.Amount) as 'Total'
                FROM Donations d
                JOIN Members m ON d.MemberId=m.Id
                WHERE strftime('%Y',d.Date)=$yr
                GROUP BY m.Id {groupBy}
                ORDER BY m.EnvelopeNumber {(period != "Annual" ? ", 3" : "")}";
            using var r = cmd.ExecuteReader();
            dt.Load(r);
            return SanitizeForGrid(dt);
        }

        public static DataTable GetReportByOfferingType(int year, string period)
        {
            EnsureDatabase();
            var dt = new DataTable();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.Parameters.AddWithValue("$yr", year.ToString());

            string periodCol = period switch
            {
                "Monthly" => "strftime('%m',d.Date) as 'Month',",
                "Quarterly" => "CASE CAST(strftime('%m',d.Date) AS INTEGER) WHEN 1 THEN 'Q1' WHEN 2 THEN 'Q1' WHEN 3 THEN 'Q1' WHEN 4 THEN 'Q2' WHEN 5 THEN 'Q2' WHEN 6 THEN 'Q2' WHEN 7 THEN 'Q3' WHEN 8 THEN 'Q3' WHEN 9 THEN 'Q3' ELSE 'Q4' END as 'Quarter',",
                _ => ""
            };

            string groupBy = period switch
            {
                "Monthly" => ", strftime('%m',d.Date)",
                "Quarterly" => ", CASE CAST(strftime('%m',d.Date) AS INTEGER) WHEN 1 THEN 1 WHEN 2 THEN 1 WHEN 3 THEN 1 WHEN 4 THEN 2 WHEN 5 THEN 2 WHEN 6 THEN 2 WHEN 7 THEN 3 WHEN 8 THEN 3 WHEN 9 THEN 3 ELSE 4 END",
                _ => ""
            };

            cmd.CommandText = $@"SELECT
                CASE WHEN ot.Description != '' THEN ot.Description ELSE ot.Name END as 'Offering Type',
                {periodCol}
                SUM(d.Amount) as 'Total'
                FROM Donations d
                JOIN OfferingTypes ot ON d.OfferingTypeId=ot.Id
                WHERE strftime('%Y',d.Date)=$yr
                GROUP BY ot.Id {groupBy}
                ORDER BY ot.Description, ot.Name";
            using var r = cmd.ExecuteReader();
            dt.Load(r);
            return SanitizeForGrid(dt);
        }

        public static DataTable GetReportTaxReceiptSummary(int year)
        {
            EnsureDatabase();
            var dt = new DataTable();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.Parameters.AddWithValue("$yr", year.ToString());
            cmd.CommandText = @"SELECT
                m.Id as 'MemberId',
                m.EnvelopeNumber as 'Env #',
                m.FirstName||' '||m.LastName as 'Member',
                m.StreetAddress as 'Address',
                m.City, m.Province, m.PostalCode as 'Postal',
                SUM(d.Amount) as 'Receipt Total'
                FROM Donations d
                JOIN Members m ON d.MemberId=m.Id
                JOIN OfferingTypes ot ON d.OfferingTypeId=ot.Id
                WHERE strftime('%Y',d.Date)=$yr
                AND (ot.TaxReceiptable=1 OR ot.TaxReceiptable IS NULL)
                GROUP BY m.Id
                HAVING SUM(d.Amount) > 0
                ORDER BY m.EnvelopeNumber";
            using var r = cmd.ExecuteReader();
            dt.Load(r);
            return SanitizeForGrid(dt);
        }

        // ── Church Settings ──────────────────────────────────────────────────

        public static ChurchSettings GetChurchSettings()
        {
            EnsureDatabase();
            var s = new ChurchSettings();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Key, Value FROM ChurchSettings";
            using var r = cmd.ExecuteReader();
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (r.Read())
                dict[r.GetString(0)] = r.IsDBNull(1) ? "" : r.GetString(1);

            s.ChurchName = dict.GetValueOrDefault("ChurchName", "");
            s.Address = dict.GetValueOrDefault("Address", "");
            s.City = dict.GetValueOrDefault("City", "");
            s.Province = dict.GetValueOrDefault("Province", "");
            s.PostalCode = dict.GetValueOrDefault("PostalCode", "");
            s.RegNumber = dict.GetValueOrDefault("RegNumber", "");
            s.AuthorizedSigner = dict.GetValueOrDefault("AuthorizedSigner", "");
            s.LogoPath = dict.GetValueOrDefault("LogoPath", "");
            s.NextReceiptNumber = int.TryParse(dict.GetValueOrDefault("NextReceiptNumber", "1"), out int n) ? n : 1;
            return s;
        }

        public static void SaveChurchSettings(ChurchSettings s)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var tran = conn.BeginTransaction();
            void Set(string key, string value)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT OR REPLACE INTO ChurchSettings (Key, Value) VALUES ($k, $v)";
                cmd.Parameters.AddWithValue("$k", key);
                cmd.Parameters.AddWithValue("$v", value);
                cmd.ExecuteNonQuery();
            }
            Set("ChurchName", s.ChurchName);
            Set("Address", s.Address);
            Set("City", s.City);
            Set("Province", s.Province);
            Set("PostalCode", s.PostalCode);
            Set("RegNumber", s.RegNumber);
            Set("AuthorizedSigner", s.AuthorizedSigner);
            Set("LogoPath", s.LogoPath);
            Set("NextReceiptNumber", s.NextReceiptNumber.ToString());
            tran.Commit();
        }

        public static int GetAndIncrementReceiptNumber()
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var tran = conn.BeginTransaction();
            using var get = conn.CreateCommand();
            get.CommandText = "SELECT Value FROM ChurchSettings WHERE Key='NextReceiptNumber'";
            var raw = get.ExecuteScalar();
            int num = raw == null || raw == DBNull.Value ? 1 : int.Parse(raw.ToString()!);
            using var set = conn.CreateCommand();
            set.CommandText = "INSERT OR REPLACE INTO ChurchSettings (Key,Value) VALUES ('NextReceiptNumber',$v)";
            set.Parameters.AddWithValue("$v", (num + 1).ToString());
            set.ExecuteNonQuery();
            tran.Commit();
            return num;
        }

        public static void RecordReceipt(int receiptNum, int memberId, int taxYear, decimal total, string pdfPath)
        {
            EnsureDatabase();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Receipts (ReceiptNumber, MemberId, TaxYear, TotalAmount, DateIssued, PdfPath)
                VALUES ($rn, $mid, $yr, $tot, $dt, $pdf)";
            cmd.Parameters.AddWithValue("$rn", receiptNum);
            cmd.Parameters.AddWithValue("$mid", memberId);
            cmd.Parameters.AddWithValue("$yr", taxYear);
            cmd.Parameters.AddWithValue("$tot", (double)total);
            cmd.Parameters.AddWithValue("$dt", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$pdf", pdfPath);
            cmd.ExecuteNonQuery();
        }

        // ── CSV Import — Dynamic ─────────────────────────────────────────────
        //
        // Both import methods read ALL headers from the CSV. Known headers are
        // mapped to the correct DB column names. Any unrecognised header is
        // sanitised and added to the table automatically (ALTER TABLE … ADD COLUMN).
        // The return value reports how many rows were inserted / updated and which
        // new columns were created so the caller can show the user what happened.

        public record ImportResult(int Inserted, int Updated, string[] NewColumns);

        // Maps every recognised Members CSV header (lower-cased) → DB column name.
        // "_FullName" is a sentinel: split into FirstName / LastName during import.
        private static readonly Dictionary<string, string> MemberAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["envelope #"]      = "EnvelopeNumber",
                ["envelope number"] = "EnvelopeNumber",
                ["envelope_number"] = "EnvelopeNumber",
                ["envelopeno"]      = "EnvelopeNumber",
                ["envelope_no"]     = "EnvelopeNumber",
                ["envelopeid"]      = "EnvelopeNumber",
                ["name"]            = "_FullName",
                ["full name"]       = "_FullName",
                ["fullname"]        = "_FullName",
                ["displayname"]     = "_FullName",
                ["first"]           = "FirstName",
                ["first name"]      = "FirstName",
                ["firstname"]       = "FirstName",
                ["givenname"]       = "FirstName",
                ["given name"]      = "FirstName",
                ["last"]            = "LastName",
                ["last name"]       = "LastName",
                ["lastname"]        = "LastName",
                ["surname"]         = "LastName",
                ["street address"]  = "StreetAddress",
                ["street"]          = "StreetAddress",
                ["address"]         = "StreetAddress",
                ["addr"]            = "StreetAddress",
                ["city"]            = "City",
                ["province"]        = "Province",
                ["state"]           = "Province",
                ["postal code"]     = "PostalCode",
                ["postal"]          = "PostalCode",
                ["postcode"]        = "PostalCode",
                ["zip"]             = "PostalCode",
                ["home phone"]      = "HomePhone",
                ["phone"]           = "HomePhone",
                ["phone number"]    = "HomePhone",
                ["email"]           = "Email",
                ["e-mail"]          = "Email",
                ["full member"]     = "FullMember",
                ["fullmember"]      = "FullMember",
                ["shut in"]         = "ShutIn",
                ["shutin"]          = "ShutIn",
                ["shut-in"]         = "ShutIn",
                ["active"]          = "Active",
                ["status"]          = "Status",
                ["memberstatus"]    = "Status",
            };

        private static readonly Dictionary<string, string> OfferingAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["offering id"]     = "Name",
                ["offering_id"]     = "Name",
                ["offering"]        = "Name",
                ["id"]              = "Name",
                ["code"]            = "Name",
                ["name"]            = "Name",
                ["description"]     = "Description",
                ["desc"]            = "Description",
                ["tax receiptable"] = "TaxReceiptable",
                ["taxreceiptable"]  = "TaxReceiptable",
            };

        public static ImportResult ImportMembersFromCsv(string csvPath)
        {
            EnsureDatabase();
            var lines = File.ReadAllLines(csvPath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (lines.Length == 0) return new(0, 0, []);

            var rawHeaders = SplitCsvLine(lines[0]);

            // Build column map: csv index → db column name (or "_FullName" sentinel)
            var colMap = rawHeaders.Select(h =>
                MemberAliases.TryGetValue(h.Trim(), out var mapped) ? mapped : SanitizeColName(h)
            ).ToArray();

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            // Ensure every non-sentinel column exists in Members
            var newCols = EnsureDynamicColumns(conn, "Members", colMap
                .Where(c => c != "_FullName" && !string.IsNullOrEmpty(c))
                .Distinct().ToArray());

            int inserted = 0, updated = 0;
            using var tran = conn.BeginTransaction();

            for (int i = 1; i < lines.Length; i++)
            {
                var vals = SplitCsvLine(lines[i]);

                // Gather all mapped values
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int j = 0; j < colMap.Length && j < vals.Length; j++)
                {
                    if (!string.IsNullOrEmpty(colMap[j]))
                        row[colMap[j]] = vals[j].Trim();
                }

                // Handle Name → First / Last split
                if (row.TryGetValue("_FullName", out var fullName) && fullName.Length > 0)
                {
                    if (!row.ContainsKey("FirstName") && !row.ContainsKey("LastName"))
                    {
                        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        row["FirstName"] = parts.Length == 1 ? parts[0] : string.Join(' ', parts[..^1]);
                        row["LastName"]  = parts.Length >= 2 ? parts[^1] : "";
                    }
                    row.Remove("_FullName");
                }

                string env   = row.GetValueOrDefault("EnvelopeNumber", "");
                string first = row.GetValueOrDefault("FirstName", "");
                string last  = row.GetValueOrDefault("LastName", "");
                if (string.IsNullOrEmpty(env) && string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last)) continue;

                // Remove sentinel from write set
                var writeRow = row.Where(kv => kv.Key != "_FullName" && !string.IsNullOrEmpty(kv.Key)).ToList();

                if (!string.IsNullOrEmpty(env))
                {
                    // Try UPDATE first
                    var setClauses = writeRow.Where(kv => kv.Key != "EnvelopeNumber")
                                             .Select((kv, n) => $"{kv.Key}=$p{n}").ToList();
                    if (setClauses.Count > 0)
                    {
                        using var upd = conn.CreateCommand();
                        upd.CommandText = $"UPDATE Members SET {string.Join(",", setClauses)} WHERE EnvelopeNumber=$env";
                        upd.Parameters.AddWithValue("$env", env);
                        var updCols = writeRow.Where(kv => kv.Key != "EnvelopeNumber").ToList();
                        for (int n = 0; n < updCols.Count; n++)
                            upd.Parameters.AddWithValue($"$p{n}", updCols[n].Value);
                        if (upd.ExecuteNonQuery() > 0) { updated++; continue; }
                    }
                }

                // INSERT
                var insCols = writeRow.Select((kv, n) => (kv.Key, kv.Value, n)).ToList();
                using var ins = conn.CreateCommand();
                ins.CommandText = $"INSERT INTO Members ({string.Join(",", insCols.Select(x => x.Key))}) " +
                                  $"VALUES ({string.Join(",", insCols.Select(x => $"$p{x.n}"))})";
                foreach (var (_, val, n) in insCols) ins.Parameters.AddWithValue($"$p{n}", val);
                ins.ExecuteNonQuery();
                inserted++;
            }

            tran.Commit();
            return new(inserted, updated, newCols);
        }

        public static ImportResult ImportOfferingTypesFromCsv(string csvPath)
        {
            EnsureDatabase();
            var lines = File.ReadAllLines(csvPath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (lines.Length == 0) return new(0, 0, []);

            var rawHeaders = SplitCsvLine(lines[0]);
            var colMap = rawHeaders.Select(h =>
                OfferingAliases.TryGetValue(h.Trim(), out var mapped) ? mapped : SanitizeColName(h)
            ).ToArray();

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var newCols = EnsureDynamicColumns(conn, "OfferingTypes", colMap
                .Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray());

            int inserted = 0, updated = 0;
            using var tran = conn.BeginTransaction();

            for (int i = 1; i < lines.Length; i++)
            {
                var vals = SplitCsvLine(lines[i]);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int j = 0; j < colMap.Length && j < vals.Length; j++)
                    if (!string.IsNullOrEmpty(colMap[j])) row[colMap[j]] = vals[j].Trim();

                string code = row.GetValueOrDefault("Name", "");
                string desc = row.GetValueOrDefault("Description", "");
                if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(desc)) continue;

                // Default TaxReceiptable to 1 if not provided
                if (!row.ContainsKey("TaxReceiptable")) row["TaxReceiptable"] = "1";
                else row["TaxReceiptable"] = row["TaxReceiptable"].Trim().ToUpperInvariant() is "Y" or "YES" or "1" or "TRUE" ? "1" : "0";

                var writeRow = row.ToList();
                var setClauses = writeRow.Where(kv => kv.Key != "Name")
                                         .Select((kv, n) => $"{kv.Key}=$p{n}").ToList();
                if (setClauses.Count > 0 && !string.IsNullOrEmpty(code))
                {
                    using var upd = conn.CreateCommand();
                    upd.CommandText = $"UPDATE OfferingTypes SET {string.Join(",", setClauses)} WHERE Name=$name";
                    upd.Parameters.AddWithValue("$name", code);
                    var updCols = writeRow.Where(kv => kv.Key != "Name").ToList();
                    for (int n = 0; n < updCols.Count; n++) upd.Parameters.AddWithValue($"$p{n}", updCols[n].Value);
                    if (upd.ExecuteNonQuery() > 0) { updated++; continue; }
                }

                var insCols = writeRow.Select((kv, n) => (kv.Key, kv.Value, n)).ToList();
                using var ins = conn.CreateCommand();
                ins.CommandText = $"INSERT OR IGNORE INTO OfferingTypes ({string.Join(",", insCols.Select(x => x.Key))}) " +
                                  $"VALUES ({string.Join(",", insCols.Select(x => $"$p{x.n}"))})";
                foreach (var (_, val, n) in insCols) ins.Parameters.AddWithValue($"$p{n}", val);
                ins.ExecuteNonQuery();
                inserted++;
            }

            tran.Commit();
            return new(inserted, updated, newCols);
        }

        // Adds any columns from `desired` that don't already exist in `table`.
        // Returns the names of columns that were actually created.
        private static string[] EnsureDynamicColumns(SqliteConnection conn, string table, string[] desired)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var p = conn.CreateCommand())
            {
                p.CommandText = $"PRAGMA table_info('{table}')";
                using var r = p.ExecuteReader();
                while (r.Read()) existing.Add(r.GetString(1));
            }
            var created = new List<string>();
            foreach (var col in desired)
            {
                if (string.IsNullOrWhiteSpace(col) || existing.Contains(col)) continue;
                using var a = conn.CreateCommand();
                a.CommandText = $"ALTER TABLE {table} ADD COLUMN [{col}] TEXT";
                a.ExecuteNonQuery();
                created.Add(col);
                existing.Add(col);
            }
            return created.ToArray();
        }

        // Converts any string into a valid SQLite column name.
        private static string SanitizeColName(string header)
        {
            var s = System.Text.RegularExpressions.Regex.Replace(header.Trim(), @"[^a-zA-Z0-9_]", "_").Trim('_');
            return string.IsNullOrEmpty(s) ? "_col" : s;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        // Microsoft.Data.Sqlite returns byte[] for any value stored with BLOB
        // affinity, even in TEXT-declared columns.  DataGridView auto-promotes
        // byte[] DataColumns to ImageColumns and then crashes trying to render
        // string data into them.  Convert every byte[] column to string after load.
        public static DataTable SanitizeForGrid(DataTable dt)
        {
            var blobNames = dt.Columns
                .Cast<DataColumn>()
                .Where(c => c.DataType == typeof(byte[]))
                .Select(c => c.ColumnName)
                .ToList();

            foreach (var name in blobNames)
            {
                int ordinal = dt.Columns[name]!.Ordinal;
                string tempName = name + "__s";
                dt.Columns.Add(tempName, typeof(string));
                foreach (DataRow row in dt.Rows)
                    row[tempName] = row[name] is byte[] b
                        ? System.Text.Encoding.UTF8.GetString(b)
                        : row[name]?.ToString() ?? "";
                dt.Columns.Remove(name);
                dt.Columns[tempName]!.ColumnName = name;
                dt.Columns[name]!.SetOrdinal(ordinal);
            }
            return dt;
        }

        private static string Col(string[] cols, int idx) =>
            idx >= 0 && idx < cols.Length ? cols[idx].Trim() : "";

        private static int FindIndex(string[] header, string[] candidates)
        {
            for (int i = 0; i < header.Length; i++)
                foreach (var c in candidates)
                    if (header[i].Contains(c)) return i;
            return -1;
        }

        private static string[] SplitCsvLine(string line)
        {
            var parts = new List<string>();
            bool inQ = false;
            var cur = new System.Text.StringBuilder();
            foreach (char ch in line)
            {
                if (ch == '"') { inQ = !inQ; continue; }
                if (ch == ',' && !inQ) { parts.Add(cur.ToString()); cur.Clear(); }
                else cur.Append(ch);
            }
            parts.Add(cur.ToString());
            return parts.ToArray();
        }
    }
}
