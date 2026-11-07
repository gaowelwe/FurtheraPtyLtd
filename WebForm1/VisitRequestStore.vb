Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient

' Saves and reads visit requests. Creates the database and table on first use.
Public Module VisitRequestStore

    Private ReadOnly ConnStr As String =
        ConfigurationManager.ConnectionStrings("MaisonUmberDb").ConnectionString
    Private ReadOnly LockObj As New Object()
    Private ready As Boolean

    Private Sub EnsureDatabase()
        If ready Then Return
        SyncLock LockObj
            If ready Then Return

            ' 1) Try to create the database (skipped quietly on hosts that already provide it)
            Try
                Dim b As New SqlConnectionStringBuilder(ConnStr)
                Dim dbName As String = b.InitialCatalog
                b.InitialCatalog = "master"
                Using cn As New SqlConnection(b.ConnectionString)
                    cn.Open()
                    Using cmd As New SqlCommand(
                    "IF DB_ID(@db) IS NULL BEGIN " &
                    "DECLARE @sql NVARCHAR(400) = N'CREATE DATABASE ' + QUOTENAME(@db); " &
                    "EXEC(@sql); " &
                    "END", cn)
                        cmd.Parameters.AddWithValue("@db", dbName)
                        cmd.ExecuteNonQuery()
                    End Using
                End Using
            Catch
                ' The host already provides the database.
            End Try

            ' 2) Create the table if it does not exist yet
            Dim sql As String =
            "IF OBJECT_ID('dbo.VisitRequests','U') IS NULL " &
            "CREATE TABLE dbo.VisitRequests (" &
            " Id INT IDENTITY(1,1) PRIMARY KEY," &
            " FullName NVARCHAR(80) NOT NULL," &
            " Email NVARCHAR(254) NOT NULL," &
            " Interest NVARCHAR(40) NOT NULL," &
            " Message NVARCHAR(600) NULL," &
            " CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME())"
            Using cn As New SqlConnection(ConnStr)
                cn.Open()
                Using cmd As New SqlCommand(sql, cn)
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            ready = True
        End SyncLock
    End Sub

    Public Function Save(fullName As String, email As String, interest As String, message As String) As Integer
        EnsureDatabase()
        Using cn As New SqlConnection(ConnStr)
            cn.Open()
            Using cmd As New SqlCommand(
                "INSERT INTO dbo.VisitRequests (FullName, Email, Interest, Message) OUTPUT INSERTED.Id VALUES (@n, @e, @i, @m)", cn)
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 80).Value = fullName
                cmd.Parameters.Add("@e", SqlDbType.NVarChar, 254).Value = email
                cmd.Parameters.Add("@i", SqlDbType.NVarChar, 40).Value = interest
                cmd.Parameters.Add("@m", SqlDbType.NVarChar, 600).Value =
                    If(String.IsNullOrWhiteSpace(message), CObj(DBNull.Value), CObj(message))
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    Public Function GetById(id As Integer) As DataRow
        EnsureDatabase()
        Dim dt As New DataTable()
        Using cn As New SqlConnection(ConnStr)
            Using da As New SqlDataAdapter(
                "SELECT Id, FullName, Email, Interest, Message, CreatedUtc FROM dbo.VisitRequests WHERE Id = @id", cn)
                da.SelectCommand.Parameters.Add("@id", SqlDbType.Int).Value = id
                da.Fill(dt)
            End Using
        End Using
        Return If(dt.Rows.Count > 0, dt.Rows(0), Nothing)
    End Function

    Public Function GetAll() As DataTable
        EnsureDatabase()
        Dim dt As New DataTable()
        Using cn As New SqlConnection(ConnStr)
            Using da As New SqlDataAdapter(
                "SELECT Id, FullName, Email, Interest, Message, CreatedUtc FROM dbo.VisitRequests ORDER BY CreatedUtc DESC", cn)
                da.Fill(dt)
            End Using
        End Using
        Return dt
    End Function

End Module