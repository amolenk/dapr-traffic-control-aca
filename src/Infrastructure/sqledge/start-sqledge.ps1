docker run -d --name dtc-sql `
  -e 'ACCEPT_EULA=Y' `
  -e 'MSSQL_SA_PASSWORD=Pass@word' `
  -p 1433:1433 `
   mcr.microsoft.com/azure-sql-edge