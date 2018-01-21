# DotNet Utilities MsSql
Basic utilities for connect to MsSql Server and performs CRUD operations oriented to using objects.

### Abstract
This library exposes functionalities to work over a Microsoft Sql Server connection, allows you to execute typical CRUD operations and perform processes over results to use them like an objects, similar to an ORM.

### Resumen
Esta librería expone funcionalidades para trabajar sobre una conexión a Microsoft Sql Server, permite ejecutar operaciones típicas CRUD y realizar un tratamiento sobre los resultados para el uso como objetos, similar a un ORM.


## API

### DbConnector
Base class for connect to MsSql server and execute T-SQL statements and return raw data and/or mapped objects.

**To view a complete documentation of methods refer to code**


## How to use

Configures your own database DbConnector class

```csharp
public class DbConnectorMyDb : DbConnector
{
  public static readonly string MyDbConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=lextract;Password=123456;";
  public DbConnectorMyDb() : base(MyDbConnectionString)
  { }
}
```

In a typical multilayer design you have a Data Access Layer (DAL) class, we assume that we have a `CititesDAL` class that manages operations for a `City` entities on database, thus, some operations could be:

```csharp
public class CitesDAL
{
  public static List<City> CititesByCountry(int idCountry)
  {
    using (DbConnectorMyDb db = new DbConnectorMyDb())
    {
      db.SetProcedure("dbo.CititesByCountry");
      db.AddParameterValue("@idCountry", 1001);
      return db.ExecuteReaderMapping<City>();
    }
  }
  public static int CreateCity(int idCountry, string name)
  {
    using (DbConnectorMyDb db = new DbConnectorMyDb())
    {
      db.SetProcedure("dbo.CitiesCreate");
      db.AddParameterValue("@idCountry", 1001);
      db.AddParameterValue("@name", name);
      return Convert.ToInt32(db.ExecuteScalar()); // returns id of new city
    }
  }
}
```

Where a `City` entity has the properties that you are interested, for example `Id` and `Name` fields should be returned in select statement of procedure, also, you migth change `db.SetProcedure` and `db.AddParameterValue` lines by `db.SetCommand("select Id, Name from cities where id = " + idCountry);` and obtain the same results.




