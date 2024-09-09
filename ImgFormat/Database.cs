using Microsoft.Data.Sqlite;
using System.Data.Common;
using Microsoft.AspNetCore.Http;

namespace ImgFormat;

/*
This 'class' is more of a namespace for database-related static variables &
functions.

We're using an in-memory database so that data doesn't persist between runs,
to make testing easier.
An on-disk database would be more suitable for production use, but that's not
a concern during testing.
*/
public static class Database
{
    /*
    Default name for a database connection.
    Alternate names must be supported so that unit tests don't interfere with
    each other.
    */
    public const string DB_NAME = "ImgFormat";

    /*
    Default limit on the number of images to keep.
    Keeps the database from using too much memory.
    Not a good idea for production, just a safety measure for this demo.
    */
    public const int DEFAULT_LIMIT = 10;

    // Create a DB connection.
    public static SqliteConnection GetConnection(string db_name = DB_NAME)
    {
        return new SqliteConnection(
            $"Data Source={db_name};Mode=Memory;Cache=Shared;Foreign Keys=True");
    }

    /*
    Initialize the database, and return the connection object.

    An "image" encompasses all of the different formats & resolutions that are
    associated with a particular upload.
    Each "file" contains image data of a specific resolution and format.

    A configurable limit keeps the database from using too much memory.
    */
    public static SqliteConnection InitDB(string db_name = DB_NAME, int limit = DEFAULT_LIMIT)
    {
        var db = GetConnection(db_name);
        db.Open();
        var command = db.CreateCommand();
        command.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS _Variables (
                name TEXT PRIMARY KEY NOT NULL,
                value
            );

            INSERT OR IGNORE INTO _Variables (
                name, value
            ) VALUES (
                'ImageLimit', $limit
            );

            CREATE TABLE IF NOT EXISTS Images (
                id INTEGER PRIMARY KEY NOT NULL,
                name TEXT NOT NULL,
                desc TEXT,
                uploaded NOT NULL
            );

            CREATE INDEX IF NOT EXISTS Idx_ImageName ON Images ( name );

            CREATE TRIGGER IF NOT EXISTS CullImages
            AFTER INSERT ON Images
            FOR EACH ROW
            BEGIN
                DELETE FROM Images WHERE id NOT IN (
                    SELECT id FROM Images
                    ORDER BY uploaded DESC, id DESC
                    LIMIT (
                        SELECT value FROM _Variables WHERE name='ImageLimit'
                    )
                );
            END;

            CREATE TABLE IF NOT EXISTS Files (
                image INTEGER NOT NULL,
                type TEXT NOT NULL,
                res TEXT NOT NULL,
                body BLOB NOT NULL,
                FOREIGN KEY ( image ) REFERENCES Images (id) ON DELETE CASCADE
            );
        ";
        command.Parameters.AddWithValue("$limit", limit);
        command.ExecuteNonQuery();
        return db;
    }

    /*
    Create a new image.

    Most fields in this table are auto-generated, so we only have to specify
    the image's user-provided description.
    */
    public static Int64? CreateImage(string description, string db_name = DB_NAME)
    {
        using (var db = GetConnection(db_name))
        {
            db.Open();
            var command = db.CreateCommand();
            command.CommandText =
            @"
                INSERT INTO Images (
                    name, desc, uploaded
                ) VALUES (
                    hex(randomblob(16)), $desc, strftime('%Y-%m-%d %H:%M:%f', 'now')
                ) RETURNING id;
            ";
            command.Parameters.AddWithValue("$desc", description);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    return reader.GetInt64(0);
                }
            }
        }
        return null;
    }

    public static void StoreImageFile(
        Int64 image_id,
        string res,
        string type,
        Byte[] body,
        string db_name = DB_NAME)
    {
        using (var db = GetConnection(db_name))
        {
            db.Open();
            var command = db.CreateCommand();
            command.CommandText =
            @"
                INSERT INTO Files (
                    image, res, type, body
                ) VALUES (
                    $image_id, $res, $type, $body
                )
            ";
            command.Parameters.AddWithValue("$image_id", image_id);
            command.Parameters.AddWithValue("$res", res);
            command.Parameters.AddWithValue("$type", type);
            command.Parameters.AddWithValue("$body", body);
            command.ExecuteNonQuery();
        }
    }

    public static IEnumerable<Tuple<string, string>> ListImages(string db_name = DB_NAME)
    {
        var result = new List<Tuple<string, string>>();
        using (var db = GetConnection(db_name))
        {
            db.Open();
            var command = db.CreateCommand();
            command.CommandText = @" SELECT name, desc FROM Images ";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    var desc = reader.GetString(1);
                    result.Add(new Tuple<string, string>(name, desc));
                }
            }
        }
        return result;
    }

    public static IEnumerable<Tuple<string, string, string, string>> ListFiles(
        string db_name = DB_NAME)
    {
        var result = new List<Tuple<string, string, string, string>>();
        using (var db = GetConnection(db_name))
        {
            db.Open();
            var command = db.CreateCommand();
            command.CommandText =
            @"
                SELECT Images.name, Images.desc, Files.res, Files.type
                FROM Files INNER JOIN Images ON Files.image = Images.id
                ORDER BY Images.uploaded, Files.type, Files.res
            ";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    var desc = reader.GetString(1);
                    var res = reader.GetString(2);
                    var type = reader.GetString(3);
                    result.Add(new Tuple<string, string, string, string>(
                        name, desc, res, type));
                }
            }
        }
        return result;
    }

    public static IResult GetImageFile(string name, string res, string type,
        string db_name = DB_NAME)
    {
        var filename = Images.FileName(name, res, type);
        using (var db = GetConnection(db_name))
        {
            db.Open();
            var command = db.CreateCommand();
            command.CommandText =
            @"
                SELECT Files.body
                FROM Files INNER JOIN Images ON Files.image = Images.id
                WHERE Images.name = $name
                AND Files.res = $res
                AND Files.type = $type
            ";
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$res", res);
            command.Parameters.AddWithValue("$type", type);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var body = reader.GetStream(0);
                    return Results.File(body, type, filename);
                }
            }
        }
        return Results.NotFound();
    }
}
