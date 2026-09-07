using Microsoft.Data.SqlClient;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string GetConnectionString()
{
    return app.Configuration.GetConnectionString("StudentDB")
        ?? throw new InvalidOperationException(
            "StudentDB connection string not found.");
}

// READ: Display all students
app.MapGet("/", async () =>
{
    var rows = new StringBuilder();

    await using var connection =
        new SqlConnection(GetConnectionString());

    await connection.OpenAsync();

    const string sql = """
        SELECT AppID, Name, Email, Course,
               ISNULL(ContactNumber, '')
        FROM Students
        ORDER BY AppID
        """;

    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        int id = reader.GetInt32(0);
        string name = WebUtility.HtmlEncode(reader.GetString(1));
        string email = WebUtility.HtmlEncode(reader.GetString(2));
        string course = WebUtility.HtmlEncode(reader.GetString(3));
        string contact = WebUtility.HtmlEncode(reader.GetString(4));

        rows.Append($$"""
            <tr>
                <td>{{id}}</td>

                <td>
                    <input form="edit-{{id}}" name="name"
                           value="{{name}}" required>
                </td>

                <td>
                    <input form="edit-{{id}}" name="email"
                           value="{{email}}" required>
                </td>

                <td>
                    <input form="edit-{{id}}" name="course"
                           value="{{course}}" required>
                </td>

                <td>
                    <input form="edit-{{id}}" name="contactNumber"
                           value="{{contact}}" required>
                </td>

                <td class="actions">
                    <form id="edit-{{id}}" method="post" action="/edit">
                        <input type="hidden" name="appId" value="{{id}}">
                        <button class="update" type="submit">Update</button>
                    </form>

                    <form method="post" action="/delete">
                        <input type="hidden" name="appId" value="{{id}}">
                        <button class="delete" type="submit">Delete</button>
                    </form>
                </td>
            </tr>
            """);
    }

    var html = $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <title>Student Management System</title>

            <style>
                body {
                    font-family: Arial;
                    background: #eef6fc;
                    margin: 30px;
                }

                .container {
                    max-width: 1200px;
                    margin: auto;
                    background: white;
                    padding: 25px;
                    border-radius: 12px;
                    box-shadow: 0 4px 15px #aaa;
                }

                h1, h2 {
                    color: #0078d4;
                    text-align: center;
                }

                .add-form {
                    display: grid;
                    grid-template-columns: repeat(5, 1fr);
                    gap: 10px;
                    margin-bottom: 25px;
                }

                input, button {
                    padding: 9px;
                    box-sizing: border-box;
                }

                .add {
                    background: #0078d4;
                    color: white;
                    border: none;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                }

                th, td {
                    border: 1px solid #ccc;
                    padding: 8px;
                }

                th {
                    background: #0078d4;
                    color: white;
                }

                td input {
                    width: 100%;
                }

                .actions {
                    display: flex;
                    gap: 5px;
                }

                .update {
                    background: #f0ad4e;
                    color: white;
                    border: none;
                }

                .delete {
                    background: #d9534f;
                    color: white;
                    border: none;
                }
            </style>
        </head>

        <body>
            <div class="container">
                <h1>Keval Student Web App</h1>
                <h2>Azure App Service + Azure SQL Database</h2>

                <form class="add-form" method="post" action="/add">
                    <input name="name" placeholder="Name" required>
                    <input name="email" type="email"
                           placeholder="Email" required>
                    <input name="course" placeholder="Course" required>
                    <input name="contactNumber"
                           placeholder="Contact Number" required>
                    <button class="add" type="submit">Add Student</button>
                </form>

                <table>
                    <tr>
                        <th>AppID</th>
                        <th>Name</th>
                        <th>Email</th>
                        <th>Course</th>
                        <th>Contact Number</th>
                        <th>Actions</th>
                    </tr>

                    {{rows}}
                </table>
            </div>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html");
});

// CREATE: Insert a student
app.MapPost("/add", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();

    await using var connection =
        new SqlConnection(GetConnectionString());

    await connection.OpenAsync();

    const string sql = """
        INSERT INTO Students
        (Name, Email, Course, ContactNumber)
        VALUES
        (@Name, @Email, @Course, @ContactNumber)
        """;

    await using var command = new SqlCommand(sql, connection);

    command.Parameters.AddWithValue("@Name", form["name"].ToString());
    command.Parameters.AddWithValue("@Email", form["email"].ToString());
    command.Parameters.AddWithValue("@Course", form["course"].ToString());
    command.Parameters.AddWithValue(
        "@ContactNumber", form["contactNumber"].ToString());

    await command.ExecuteNonQueryAsync();

    return Results.Redirect("/");
});

// UPDATE: Edit a student
app.MapPost("/edit", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();

    await using var connection =
        new SqlConnection(GetConnectionString());

    await connection.OpenAsync();

    const string sql = """
        UPDATE Students
        SET Name = @Name,
            Email = @Email,
            Course = @Course,
            ContactNumber = @ContactNumber
        WHERE AppID = @AppID
        """;

    await using var command = new SqlCommand(sql, connection);

    command.Parameters.AddWithValue("@AppID", form["appId"].ToString());
    command.Parameters.AddWithValue("@Name", form["name"].ToString());
    command.Parameters.AddWithValue("@Email", form["email"].ToString());
    command.Parameters.AddWithValue("@Course", form["course"].ToString());
    command.Parameters.AddWithValue(
        "@ContactNumber", form["contactNumber"].ToString());

    await command.ExecuteNonQueryAsync();

    return Results.Redirect("/");
});

// DELETE: Remove a student
app.MapPost("/delete", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();

    await using var connection =
        new SqlConnection(GetConnectionString());

    await connection.OpenAsync();

    const string sql =
        "DELETE FROM Students WHERE AppID = @AppID";

    await using var command = new SqlCommand(sql, connection);

    command.Parameters.AddWithValue(
        "@AppID", form["appId"].ToString());

    await command.ExecuteNonQueryAsync();

    return Results.Redirect("/");
});

app.Run();
