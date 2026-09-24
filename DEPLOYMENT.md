# Running Al Ameer on WIN-LP90440U1LA

The app uses the local default SQL Server instance `WIN-LP90440U1LA` when it runs on that laptop. On other computers it retains the development instance `.\MSSQLSERVER03`. To choose a different instance without rebuilding, set the `AL_AMEER_SQL_SERVER` environment variable before launching the app. The database name is `al_ameer`, and the app uses Windows authentication.

1. Clone the repository once, or run `git pull origin main` in an existing clone.
2. Confirm SQL Server has an `al_ameer` database and the Windows account running the app has access. The database itself is not stored in Git.
3. Make and verify a full SQL database backup before applying migrations. Run these scripts in order against `al_ameer` (skip scripts already applied; they are idempotent):

   ```powershell
   sqlcmd -S WIN-LP90440U1LA -d al_ameer -E -b -i database\20260923_customer_employee_ledgers.sql
   sqlcmd -S WIN-LP90440U1LA -d al_ameer -E -b -i database\20260923_settings_reversals.sql
   sqlcmd -S WIN-LP90440U1LA -d al_ameer -E -b -i database\20260923_sale_tender_receipts.sql
   sqlcmd -S WIN-LP90440U1LA -d al_ameer -E -b -i database\20260924_salary_expenses_stock_adjustments.sql
   ```

4. Build the app with the .NET 10 SDK: `dotnet build al_ameer.slnx -c Release`. Run `al_ameer\bin\Release\net10.0-windows\al_ameer.exe`.

If sign-in reports a connection error, check that the SQL Server service is running, the `al_ameer` database exists, and the Windows account has database access. Do not change the database schema or copy live data without a verified backup.
