# Hrimsoft Sql Runner
![GitHub](https://img.shields.io/github/license/basim108/sql-bulk-service-postgresql)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/Basim108/sql-runner)

A database tool to execute or up or down sql scripts onto different databases.

# Usage
## Scripts folder structure
make a folder, often is a repo, with structure
```
 <database-repo-folder>
     |
     --- Up
          |
          --- <project-name>
                      |
                      --- dbname.script-name.sql
                      --- xx.dbname.ordered-script-name.sql
          --- xx.<oredered-project>
                      |
                      --- dbname.script-name.sql
                      --- xx.dbname.ordered-script-name.sql
          --- dbname.script-name.sql
          --- xx.dbname.ordered-script-name.sql
          
     --- Down
          |
          --- <project-name>
                      |
                      --- dbname.script-name.sql
                      --- xx.dbname.ordered-script-name.sql
          --- dbname.script-name.sql
          --- xx.dbname.ordered-script-name.sql
```

Folders Up and Down could be renamed in appsettings.json 
```json
{
  "UpFolderName": "Install",
  "DownFolderName": "Uninstall"
}
```
Inside Up folder you could place sql-scripts immediately or categorize them in project folders.
each folder or sql script could have a prefix as a number e.g. 
scripts
accountingdb.adding-additional-column.sql 
01.userdb.creating-profile-table.sql
02.userdb.moving-items-from-users-to-profile-table.sql

The ordered scripts will be executed first, and only after that none-ordered scripts.
For folders
```
 <database-repo-folder>
     |
     --- Up
          |
          --- integration
          --- 01.accounting
          --- 02.users
    ...
```
The ordered projects will be processed first than integration folder.


## Database configurations
At the moment only PostgreSQL database is supported.
Each sql script could be executed on its own database. The name of the database is set in the sql-script file name as prefix or after order number.
This database name is a key in appsettings.json Environment:Databases[]:Name section
For example, such a configuration might look like
```json
{
  "Environments": [
    {
      "Environment": "development",
      "Databases": [
        {
          "Name": "accountingdb",
          "ConnectionString": "Host=dev.us-west-2.rds.amazonaws.com;Port=5432;Database=accountingdb;Username=acounting_service;Pooling=True;CommandTimeout=300;",
          "Password": "some_password"
        },
        {
          "Name": "userdb",
          "ConnectionString": "Host=dev.us-west-2.rds.amazonaws.com;Port=5432;Database=userdb;Username=crm_service;Pooling=True;CommandTimeout=300;",
          "Password": "some_password"
        }
      ]
    },
    {
      "Environment": "staging",
      "Databases": [
        {
          "Name": "accountingdb",
          "ConnectionString": "Host=staging.us-west-2.rds.amazonaws.com;Port=5432;Database=accountingdb;Username=acounting_service;Pooling=True;CommandTimeout=300;",
          "Password": "some_password"
        },
        {
          "Name": "userdb",
          "ConnectionString": "Host=staging.us-west-2.rds.amazonaws.com;Port=5432;Database=userdb;Username=crm_service;Pooling=True;CommandTimeout=300;",
          "Password": "some_password"
        }
      ]
    }
  ]
}
```

So according to this configuration the runner will use one connection for each database configuration, and run all scripts that have the same database_name prefix via this single connection in one transaction.
How runner behaves in error situations see the Error Handling section of this documentation.

## Command line arguments
```console
sql-runner --help

  --up              If set than scripts will be run from up folder.
                    By default it is true

  --down            If set than scripts will be run from down folder.
                    By default it is false

  -p, --path        Required. Set path to the folder with sql scripts

  -e, --env         Set environment name:
                     -e development
                     --env staging
                     -e production

  -s, --settings    Set path to the appsetings.json file where databases, users, passwords and environments are listed
                    By default appsettings.json file will be looked at the tool working folder.

  --help            Display this help screen.

  --version         Display version information.
```

To execute sqripts from Up folder on staging environment
```console
sql-runner -p c:\git\database\ -e staging
or
sql-runner -p c:\git\database\ -e staging --up
```

To execute sqripts from Down folder on staging environment
```console
sql-runner -p c:\git\database\ -e staging  --down
```
## Error handling
If during the execution of Up-folder sql scripts an exception occurs, transactions in each opened database connection will be rollbacked.
If an error occurs after some transaction is committed, then sql scripts from the Down folder will be executed. Not all scripts from down folder, but only for those that were successfully committed during up-folder scripts execution.
